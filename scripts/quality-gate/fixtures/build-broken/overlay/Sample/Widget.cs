namespace Sample;

public static class Widget
{
    // Missing return type and a stray token: this does not compile.
    public static Add(int a, int b) => a + b @;
}
