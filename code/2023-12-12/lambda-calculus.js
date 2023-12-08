x => x;
(x => x)(2);
// TRUE
(t, f) => t;
// FALSE
(t, f) => f;
// NOT
b => b((t, f) => f, (t, f) => t);

// using assignments as aliases for succinctness without losing generality
TRUE = (t, f) => t;
FALSE = (t, f) => f;
NOT = b => b(FALSE, TRUE);

AND = (x, y) => x(y, FALSE);
OR = (x, y) => x(TRUE, y);
IF = (b, x, y) => b(x, y);

NOT(TRUE)("true", "false"); // "false"
NOT(FALSE)("true", "false"); // "true"
IF (AND(TRUE, TRUE))("true", "false"); // "true"

x => x (x)
(x => x (x)) (x => x (x)) // infinite loop

Y = f => ((x => f(x(x)))(x => f(x(x)))); // harnessing the infinite loop by calling `f` repeatedly
fact_gen = f => x => x == 0 ? 1 : x * f (x - 1);
