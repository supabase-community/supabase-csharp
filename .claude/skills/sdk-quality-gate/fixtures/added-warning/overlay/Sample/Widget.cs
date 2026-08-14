namespace Sample;

public static class Widget
{
#warning fixture: a deliberate compiler warning above the baseline of zero
    public static int Add(int a, int b) => a + b;
}
