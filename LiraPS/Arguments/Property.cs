using System;

namespace LiraPS.Arguments;

public enum Property
{
    None = 0,
    Issue = 1 << 1,
    User = 1 << 2,
    Day = 1 << 3,
    Month = 1 << 4,
    Year = 1 << 5,
}
