using System.Windows.Media;
using System.Windows.Shapes;

namespace ScreenInk.App.Overlay;

internal sealed record DetachedStroke(
    Guid StrokeId,
    Path Element,
    Path? InkCoreElement,
    double BaseOpacity,
    double InkCoreBaseOpacity,
    int CanvasIndex);
