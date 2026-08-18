namespace Sample;

// Deliberately untidy — this project must never be discovered, built, or format-
// checked at all, so nothing about its content should matter. If a future change
// makes the gate see it, this ugly formatting would also trip stage 1b, giving a
// second, independent signal of the same regression.
public static class Decoy{
public static int Add(int a,int b)=>a+b;
}
