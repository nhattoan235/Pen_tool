using ScreenInk.Core.Diagnostics;
using ScreenInk.Core.Displays;
using ScreenInk.Core.Interaction;
using ScreenInk.Core.Ink;

var tests = new (string Name, Action Run)[]
{
    ("File logger creates a UTF-8 log and writes levels", FileLoggerWritesExpectedEntries),
    ("File logger rejects writes after disposal", FileLoggerRejectsWritesAfterDisposal),
    ("Interaction mode toggles drawing and returns to pointer", InteractionModeTransitionsAreSafe),
    ("Interaction mode emits only real transitions", InteractionModeEmitsOnlyRealTransitions),
    ("Display bounds support negative desktop coordinates", DisplayBoundsSupportNegativeCoordinates),
    ("Stroke smoothing filters mouse micro-jitter", StrokeSmoothingFiltersMicroJitter),
    ("Default mouse smoothing stays responsive at normal drawing speed", DefaultMouseSmoothingStaysResponsive),
    ("Stroke buffer preserves rejected raw mouse samples", StrokeBufferPreservesRawSamples),
    ("Stroke smoothing reaches the released mouse position", StrokeSmoothingReachesFinalPosition),
    ("Stroke smoothing rejects points after completion", StrokeSmoothingRejectsPointsAfterCompletion),
    ("Live stroke preview reaches the current raw mouse position", LiveStrokePreviewReachesRawPosition),
    ("Freehand outline creates a closed body around a stroke", FreehandOutlineCreatesStrokeBody),
    ("Freehand outline creates a round dot for a click", FreehandOutlineCreatesRoundDot),
    ("Open freehand outline has round caps beyond both endpoints", OpenOutlineHasRoundCaps),
    ("Closed gesture overlaps round caps without cutting its seam", ClosedGestureOverlapsRoundCaps),
    ("Natural pen makes slow motion wider than fast motion", NaturalPenVariesWidthWithSpeed),
    ("Temporary ink remains fully visible for 1.65 seconds", TemporaryInkStaysVisible),
    ("Temporary ink fades and expires at two seconds", TemporaryInkFadesAndExpires),
    ("Nearby strokes share a lifetime group and reset it", NearbyStrokesShareLifetimeGroup),
    ("Separated strokes create independent lifetime groups", SeparatedStrokesCreateIndependentGroups),
    ("Undo removes a stroke from its lifetime group", UndoRemovesStrokeFromLifetimeGroup),
    ("Ink color cycles through the compact toolbar palette", InkColorCyclesThroughPalette),
    ("Ink color can be selected directly by number", InkColorCanBeSelectedByNumber),
    ("Ink tools expose mouse-first size presets", InkToolsExposeSizePresets),
    ("Persistent ink can be toggled independently", PersistentInkCanBeToggled),
    ("Ink tools cycle in both wheel directions", InkToolsCycleInBothDirections),
    ("Shape recognizer detects a held straight line", ShapeRecognizerDetectsLine),
    ("Shape recognizer detects a one-stroke arrow", ShapeRecognizerDetectsArrow),
    ("Shape recognizer detects a rough ellipse", ShapeRecognizerDetectsEllipse),
    ("Shape recognizer detects a rough rectangle", ShapeRecognizerDetectsRectangle),
    ("Shape recognizer rejects an open scribble", ShapeRecognizerRejectsScribble),
};

var failed = 0;
foreach (var test in tests)
{
    try
    {
        test.Run();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception exception)
    {
        failed++;
        Console.Error.WriteLine($"FAIL {test.Name}");
        Console.Error.WriteLine(exception);
    }
}

Console.WriteLine($"{tests.Length - failed}/{tests.Length} tests passed.");
return failed == 0 ? 0 : 1;

static void FileLoggerWritesExpectedEntries()
{
    using var temporaryDirectory = new TemporaryDirectory();
    var logPath = Path.Combine(temporaryDirectory.Path, "nested", "screenink.log");

    using (var logger = new FileAppLogger(logPath))
    {
        logger.Info("Application started.");
        logger.Error("Example failure.", new InvalidOperationException("Expected test exception."));
    }

    var contents = File.ReadAllText(logPath);
    AssertContains(contents, "[INFO] Application started.");
    AssertContains(contents, "[ERROR] Example failure.");
    AssertContains(contents, "Expected test exception.");
}

static void FileLoggerRejectsWritesAfterDisposal()
{
    using var temporaryDirectory = new TemporaryDirectory();
    var logger = new FileAppLogger(Path.Combine(temporaryDirectory.Path, "screenink.log"));
    logger.Dispose();

    try
    {
        logger.Info("This write should fail.");
    }
    catch (ObjectDisposedException)
    {
        return;
    }

    throw new InvalidOperationException("Expected ObjectDisposedException.");
}

static void InteractionModeTransitionsAreSafe()
{
    var controller = new InteractionModeController();
    AssertEqual(InteractionMode.Pointer, controller.Current);

    controller.ToggleDrawing();
    AssertEqual(InteractionMode.Draw, controller.Current);

    controller.ToggleDrawing();
    AssertEqual(InteractionMode.Pointer, controller.Current);

    controller.SetMode(InteractionMode.Select);
    controller.ReturnToPointer();
    AssertEqual(InteractionMode.Pointer, controller.Current);
}

static void InteractionModeEmitsOnlyRealTransitions()
{
    var controller = new InteractionModeController();
    var transitions = new List<InteractionModeChangedEventArgs>();
    controller.ModeChanged += (_, change) => transitions.Add(change);

    controller.SetMode(InteractionMode.Pointer);
    controller.SetMode(InteractionMode.Draw);
    controller.SetMode(InteractionMode.Draw);
    controller.ReturnToPointer();

    AssertEqual(2, transitions.Count);
    AssertEqual(InteractionMode.Pointer, transitions[0].Previous);
    AssertEqual(InteractionMode.Draw, transitions[0].Current);
    AssertEqual(InteractionMode.Draw, transitions[1].Previous);
    AssertEqual(InteractionMode.Pointer, transitions[1].Current);
}

static void DisplayBoundsSupportNegativeCoordinates()
{
    var bounds = new DisplayBounds(-1920, -200, 1920, 1080);
    AssertEqual(0, bounds.Right);
    AssertEqual(880, bounds.Bottom);
}

static void StrokeSmoothingFiltersMicroJitter()
{
    var stroke = new StrokePointBuffer(new StrokeSmoothingOptions
    {
        MinimumDistance = 1,
    });

    AssertEqual(true, stroke.Add(new StrokePoint(10, 10, 0)));
    AssertEqual(false, stroke.Add(new StrokePoint(10.2, 10.2, 1)));
    AssertEqual(false, stroke.Add(new StrokePoint(10.4, 10.4, 2)));
    AssertEqual(1, stroke.Points.Count);
}

static void DefaultMouseSmoothingStaysResponsive()
{
    var stroke = new StrokePointBuffer();
    stroke.Add(new StrokePoint(0, 0, 0));

    // 2 px every 8 ms matches the median speed and event interval measured
    // from the user's Phase 7 mouse corpus.
    for (var index = 1; index <= 20; index++)
    {
        stroke.Add(new StrokePoint(index * 2, 0, index * 8));
    }

    var filteredEnd = stroke.Points[^1];
    var rawEnd = stroke.RawPoints[^1];
    if (filteredEnd.DistanceTo(rawEnd) > 3)
    {
        throw new InvalidOperationException(
            $"Expected normal mouse motion to stay within 3 px, got {filteredEnd.DistanceTo(rawEnd):F2} px.");
    }
}

static void StrokeBufferPreservesRawSamples()
{
    var stroke = new StrokePointBuffer(new StrokeSmoothingOptions
    {
        MinimumDistance = 1,
    });

    stroke.Add(new StrokePoint(10, 10, 0));
    stroke.Add(new StrokePoint(10.2, 10.2, 1));
    stroke.Add(new StrokePoint(12, 12, 2));

    AssertEqual(3, stroke.RawPoints.Count);
    AssertEqual(2, stroke.Points.Count);

    stroke.Complete(new StrokePoint(12, 12, 3));
    AssertEqual(4, stroke.RawPoints.Count);
    if (stroke.Points.Count > stroke.RawPoints.Count)
    {
        throw new InvalidOperationException("Filtered samples cannot outnumber raw input samples.");
    }
}

static void StrokeSmoothingReachesFinalPosition()
{
    var stroke = new StrokePointBuffer();
    stroke.Add(new StrokePoint(0, 0, 0));
    stroke.Add(new StrokePoint(20, 10, 10));
    stroke.Add(new StrokePoint(40, 20, 20));
    stroke.Complete(new StrokePoint(50, 25, 25));

    var finalPoint = stroke.Points[^1];
    AssertEqual(50d, finalPoint.X);
    AssertEqual(25d, finalPoint.Y);
}

static void StrokeSmoothingRejectsPointsAfterCompletion()
{
    var stroke = new StrokePointBuffer();
    stroke.Add(new StrokePoint(0, 0, 0));
    stroke.Complete(new StrokePoint(1, 1, 1));

    try
    {
        stroke.Add(new StrokePoint(2, 2, 2));
    }
    catch (InvalidOperationException)
    {
        return;
    }

    throw new InvalidOperationException("Expected a completed stroke to reject new points.");
}

static void LiveStrokePreviewReachesRawPosition()
{
    var stroke = new StrokePointBuffer();
    stroke.Add(new StrokePoint(0, 0, 0));
    stroke.Add(new StrokePoint(20, 0, 20));

    var previewEnd = stroke.GetRenderablePoints()[^1];
    AssertEqual(20d, previewEnd.X);
    AssertEqual(0d, previewEnd.Y);
}

static void FreehandOutlineCreatesStrokeBody()
{
    var builder = new FreehandStrokeOutlineBuilder();
    var outline = builder.Build(
    [
        new StrokePoint(0, 0, 0),
        new StrokePoint(10, 0, 10),
        new StrokePoint(20, 0, 20),
        new StrokePoint(30, 0, 30),
    ]);

    if (outline.Count <= 8)
    {
        throw new InvalidOperationException("Expected the open outline to include round-cap points.");
    }
    if (!outline.Any(point => point.Y > 0) || !outline.Any(point => point.Y < 0))
    {
        throw new InvalidOperationException("Expected outline points on both sides of the centerline.");
    }

    AssertAllFinite(outline);
}

static void FreehandOutlineCreatesRoundDot()
{
    var builder = new FreehandStrokeOutlineBuilder();
    var outline = builder.Build([new StrokePoint(12, 18, 0)]);

    AssertEqual(16, outline.Count);
    AssertAllFinite(outline);
}

static void OpenOutlineHasRoundCaps()
{
    var points = new[]
    {
        new StrokePoint(0, 20, 0),
        new StrokePoint(10, 20, 8),
        new StrokePoint(20, 20, 16),
        new StrokePoint(30, 20, 24),
    };
    var outline = new FreehandStrokeOutlineBuilder(new FreehandStrokeOptions
    {
        Size = 8,
        Thinning = 0,
        StartTaperLength = 0,
        EndTaperLength = 0,
    }).Build(points);

    var minimumX = outline.Min(point => point.X);
    var maximumX = outline.Max(point => point.X);
    if (minimumX > -3.4 || maximumX < 33.4)
    {
        throw new InvalidOperationException(
            $"Expected 4 px round caps beyond endpoints, got X range {minimumX:F2}..{maximumX:F2}.");
    }

    AssertAllFinite(outline);
}

static void ClosedGestureOverlapsRoundCaps()
{
    var points = Enumerable.Range(0, 33)
        .Select(index =>
        {
            var angle = (Math.PI * 2 * index) / 32;
            return new StrokePoint(
                80 + (Math.Cos(angle) * 40),
                80 + (Math.Sin(angle) * 40),
                index * 8);
        })
        .ToArray();
    var outline = new FreehandStrokeOutlineBuilder(new FreehandStrokeOptions
    {
        Size = 6,
        Thinning = 0,
    }).Build(points);

    if (outline.Count <= points.Length * 2)
    {
        throw new InvalidOperationException("Expected overlapping round caps to remain at the seam.");
    }

    var seam = points[0];
    var seamNeighborhood = outline
        .Where(point => Math.Abs(point.X - seam.X) <= 7 && Math.Abs(point.Y - seam.Y) <= 7)
        .ToArray();
    if (seamNeighborhood.Min(point => point.X) > seam.X - 2.5 ||
        seamNeighborhood.Max(point => point.X) < seam.X + 2.5 ||
        seamNeighborhood.Min(point => point.Y) > seam.Y - 2.5 ||
        seamNeighborhood.Max(point => point.Y) < seam.Y + 2.5)
    {
        throw new InvalidOperationException("Expected the overlapping caps to cover the seam in every direction.");
    }

    AssertAllFinite(outline);
}

static void NaturalPenVariesWidthWithSpeed()
{
    var points = new List<StrokePoint> { new(0, 20, 0) };
    for (var index = 1; index <= 10; index++)
    {
        points.Add(new StrokePoint(index, 20, index * 16));
    }

    for (var index = 1; index <= 10; index++)
    {
        points.Add(new StrokePoint(10 + (index * 8), 20, 160 + (index * 8)));
    }

    var outline = new FreehandStrokeOutlineBuilder(new FreehandStrokeOptions
    {
        Size = 8,
        Thinning = 0.48,
        MaximumSpeed = 0.85,
        PressureSmoothing = 0.36,
    }).Build(points);

    const int capIntermediateCount = 5;
    var rightStart = points.Count + capIntermediateCount;
    double WidthAt(int pointIndex) => outline[pointIndex].DistanceTo(
        outline[rightStart + (points.Count - 1 - pointIndex)]);

    var slowWidth = WidthAt(10);
    var fastWidth = WidthAt(20);
    if (slowWidth - fastWidth < 1.5)
    {
        throw new InvalidOperationException(
            $"Expected visible natural width variation, got slow {slowWidth:F2}px and fast {fastWidth:F2}px.");
    }

    AssertAllFinite(outline);
}

static void TemporaryInkStaysVisible()
{
    var manager = new TemporaryInkLifetimeManager();
    var strokeId = Guid.NewGuid();
    manager.AddStroke(strokeId, 100);

    var beforeFade = manager.Evaluate(1_749).Single();
    var atFadeBoundary = manager.Evaluate(1_750).Single();
    AssertEqual(1d, beforeFade.Opacity);
    AssertEqual(1d, atFadeBoundary.Opacity);
    AssertEqual(false, atFadeBoundary.IsExpired);
}

static void TemporaryInkFadesAndExpires()
{
    var manager = new TemporaryInkLifetimeManager();
    manager.AddStroke(Guid.NewGuid(), 0);

    var halfway = manager.Evaluate(1_825).Single();
    AssertNear(0.5, halfway.Opacity, 0.0001);
    AssertEqual(false, halfway.IsExpired);

    var expired = manager.Evaluate(2_000).Single();
    AssertEqual(0d, expired.Opacity);
    AssertEqual(true, expired.IsExpired);
    AssertEqual(false, manager.HasActiveGroups);
}

static void NearbyStrokesShareLifetimeGroup()
{
    var manager = new TemporaryInkLifetimeManager();
    var first = manager.AddStroke(Guid.NewGuid(), 0);
    var second = manager.AddStroke(Guid.NewGuid(), 399);

    AssertEqual(first.GroupId, second.GroupId);
    AssertEqual(2, second.StrokeIds.Count);
    AssertEqual(1d, manager.Evaluate(2_048).Single().Opacity);
}

static void SeparatedStrokesCreateIndependentGroups()
{
    var manager = new TemporaryInkLifetimeManager();
    var first = manager.AddStroke(Guid.NewGuid(), 0);
    var second = manager.AddStroke(Guid.NewGuid(), 400);

    if (first.GroupId == second.GroupId)
    {
        throw new InvalidOperationException("Expected the grouping boundary to create a new group.");
    }

    AssertEqual(2, manager.Evaluate(400).Count);
}

static void UndoRemovesStrokeFromLifetimeGroup()
{
    var manager = new TemporaryInkLifetimeManager();
    var firstStroke = Guid.NewGuid();
    var secondStroke = Guid.NewGuid();
    manager.AddStroke(firstStroke, 0);
    manager.AddStroke(secondStroke, 100);

    manager.RemoveStroke(secondStroke);
    var group = manager.Evaluate(100).Single();
    AssertEqual(1, group.StrokeIds.Count);
    AssertEqual(firstStroke, group.StrokeIds[0]);
}

static void InkColorCyclesThroughPalette()
{
    var controller = new InkStyleController();
    var first = controller.CurrentColor;
    var changes = 0;
    controller.StyleChanged += (_, _) => changes++;

    for (var index = 0; index < 5; index++)
    {
        controller.CycleColor();
    }

    AssertEqual(first, controller.CurrentColor);
    AssertEqual(5, changes);
}

static void InkColorCanBeSelectedByNumber()
{
    var controller = new InkStyleController();
    var changes = 0;
    controller.StyleChanged += (_, _) => changes++;

    controller.SelectColor(2);
    AssertEqual(2, controller.CurrentColorIndex);
    AssertEqual(new InkColor(64, 145, 255), controller.CurrentColor);
    AssertEqual(controller.CurrentColor, controller.GetColor(2));

    controller.SelectColor(2);
    AssertEqual(1, changes);

    controller.SelectColor(4);
    AssertEqual(new InkColor(245, 245, 247), controller.CurrentColor);
    AssertEqual(2, changes);
}

static void InkToolsExposeSizePresets()
{
    var controller = new InkStyleController();
    AssertEqual(InkTool.Pen, controller.CurrentTool);
    AssertEqual(5d, controller.CurrentSize);

    controller.SelectTool(InkTool.Highlighter);
    AssertEqual(20d, controller.CurrentSize);
    AssertNear(0.34, controller.CurrentOpacity, 0.0001);

    controller.AdjustSize(1);
    AssertEqual(32d, controller.CurrentSize);
    controller.AdjustSize(1);
    AssertEqual(32d, controller.CurrentSize);

    controller.SelectTool(InkTool.Pen);
    AssertEqual(5d, controller.CurrentSize);
    AssertEqual(2, controller.GetSizeIndex(InkTool.Highlighter));
}

static void PersistentInkCanBeToggled()
{
    var controller = new InkStyleController();
    AssertEqual(false, controller.IsPersistent);
    controller.SetPersistent(true);
    AssertEqual(true, controller.IsPersistent);
    controller.SetPersistent(false);
    AssertEqual(false, controller.IsPersistent);
}

static void InkToolsCycleInBothDirections()
{
    var controller = new InkStyleController();
    controller.CycleTool(1);
    AssertEqual(InkTool.Highlighter, controller.CurrentTool);
    controller.CycleTool(-1);
    AssertEqual(InkTool.Pen, controller.CurrentTool);
    controller.CycleTool(-1);
    AssertEqual(InkTool.Lasso, controller.CurrentTool);

    controller.CycleTool(2);
    AssertEqual(InkTool.Highlighter, controller.CurrentTool);
    controller.CycleTool(-2);
    AssertEqual(InkTool.Lasso, controller.CurrentTool);
}

static void ShapeRecognizerDetectsLine()
{
    var points = Enumerable.Range(0, 12)
        .Select(index => new StrokePoint(index * 10, 20 + ((index % 2) * 0.6), index * 10))
        .ToArray();
    var result = new ShapeRecognizer().Recognize(points);
    AssertEqual(RecognizedShapeKind.Line, result?.Kind);
}

static void ShapeRecognizerDetectsArrow()
{
    var points = new[]
    {
        new StrokePoint(10, 60, 0),
        new StrokePoint(35, 60.5, 10),
        new StrokePoint(65, 59.5, 20),
        new StrokePoint(100, 60, 30),
        new StrokePoint(88, 48, 40),
        new StrokePoint(76, 40, 50),
        new StrokePoint(88, 50, 60),
        new StrokePoint(100, 60, 70),
        new StrokePoint(87, 72, 80),
        new StrokePoint(76, 80, 90),
    };

    var result = new ShapeRecognizer().Recognize(points);
    AssertEqual(RecognizedShapeKind.Arrow, result?.Kind);
    AssertNear(100, result?.End.X ?? double.NaN, 0.001);
    AssertNear(60, result?.End.Y ?? double.NaN, 0.001);
}

static void ShapeRecognizerDetectsEllipse()
{
    var points = Enumerable.Range(0, 33)
        .Select(index =>
        {
            var angle = (Math.PI * 2 * index) / 32;
            return new StrokePoint(
                100 + (Math.Cos(angle) * (50 + ((index % 3) - 1))),
                80 + (Math.Sin(angle) * (30 + ((index % 2) * 0.8))),
                index * 10);
        })
        .ToArray();
    var result = new ShapeRecognizer().Recognize(points);
    AssertEqual(RecognizedShapeKind.Ellipse, result?.Kind);
}

static void ShapeRecognizerDetectsRectangle()
{
    var points = new[]
    {
        new StrokePoint(10, 10, 0), new StrokePoint(50, 11, 10), new StrokePoint(90, 10, 20),
        new StrokePoint(91, 35, 30), new StrokePoint(90, 60, 40),
        new StrokePoint(50, 59, 50), new StrokePoint(10, 60, 60),
        new StrokePoint(9, 35, 70), new StrokePoint(10, 10, 80),
    };
    var result = new ShapeRecognizer().Recognize(points);
    AssertEqual(RecognizedShapeKind.Rectangle, result?.Kind);
}

static void ShapeRecognizerRejectsScribble()
{
    var points = new[]
    {
        new StrokePoint(0, 0, 0), new StrokePoint(40, 30, 10),
        new StrokePoint(5, 50, 20), new StrokePoint(45, 5, 30),
        new StrokePoint(20, 60, 40),
    };
    AssertEqual<ShapeRecognitionResult?>(null, new ShapeRecognizer().Recognize(points));
}

static void AssertAllFinite(IEnumerable<InkPoint> points)
{
    foreach (var point in points)
    {
        if (!double.IsFinite(point.X) || !double.IsFinite(point.Y))
        {
            throw new InvalidOperationException("Stroke outline contains a non-finite coordinate.");
        }
    }
}

static void AssertContains(string value, string expected)
{
    if (!value.Contains(expected, StringComparison.Ordinal))
    {
        throw new InvalidOperationException($"Expected text was not found: {expected}");
    }
}

static void AssertEqual<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected {expected}, received {actual}.");
    }
}

static void AssertNear(double expected, double actual, double tolerance)
{
    if (Math.Abs(expected - actual) > tolerance)
    {
        throw new InvalidOperationException($"Expected {expected} ± {tolerance}, received {actual}.");
    }
}

file sealed class TemporaryDirectory : IDisposable
{
    public TemporaryDirectory()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "ScreenInk.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
