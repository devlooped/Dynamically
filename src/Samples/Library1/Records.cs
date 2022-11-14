using System.Collections.Generic;

namespace Library;

public partial record Point(int X, int Y);

public partial record Line(Point Start, Point End);

public partial record Drawing(IList<Line> Lines);

public partial record OnDidDraw(Drawing Drawing);

// NOTE: non-partial as example
public record OnDidDrawLine(Line Line);