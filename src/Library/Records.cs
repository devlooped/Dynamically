namespace Library;

public record Point(int X, int Y);

public record Line(Point Start, Point End);

public record Buffer(Line[] Lines);

public record OnDidEdit(Buffer Buffer);