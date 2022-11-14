namespace Library;

public partial record Point(int X, int Y);

public partial record Line(Point Start, Point End);

public partial record Drawing(Line[] Lines);

public partial record OnDidDraw(Drawing Drawing);

public partial record OnDidDrawLine(Line Line);