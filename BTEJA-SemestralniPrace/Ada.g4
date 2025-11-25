grammar Ada;

// Hlavní program
program: procedureDeclaration EOF;

// Deklarace procedury/funkce
procedureDeclaration:
    'procedure' identifier formalParameters? 'is'
        declarations
    'begin'
        statements
    'end' identifier ';'
    |
    'function' identifier formalParameters? 'return' typeName 'is'
        declarations
    'begin'
        statements
    'end' identifier ';'
;

// Formální parametry
formalParameters: '(' parameterList ')';
parameterList: parameter (';' parameter)*;
parameter: identifierList ':' parameterMode? typeName;
parameterMode: 'in' | 'out' | 'in' 'out';
identifierList: identifier (',' identifier)*;

// Deklarace
declarations: declaration*;
declaration:
    variableDeclaration
    | typeDeclaration
    | procedureDeclaration
;

// Deklarace proměnných
variableDeclaration: identifierList ':' typeName (':=' expression)? ';';

// Deklarace typů (pro pole)
typeDeclaration: 'type' identifier 'is' typeDefinition ';';
typeDefinition: 'array' '(' indexRange (',' indexRange)* ')' 'of' typeName;
indexRange: expression '..' expression;

// Typy
typeName:
    'Integer'
    | 'Real'
    | 'String'
    | identifier
;

// Příkazy
statements: statement*;
statement:
    assignmentStatement
    | ifStatement
    | loopStatement
    | forStatement
    | procedureCall
    | returnStatement
    | exitStatement
;

// Přiřazení
assignmentStatement: variable ':=' expression ';';
variable: identifier arrayAccess?;
arrayAccess: '(' expression (',' expression)* ')';

// Podmínka
ifStatement:
    'if' expression 'then'
        statements
    ('elsif' expression 'then'
        statements)*
    ('else'
        statements)?
    'end' 'if' ';'
;

// Cyklus loop
loopStatement:
    (identifier ':')? 'loop'
        statements
    'end' 'loop' identifier? ';'
;

// Cyklus for
forStatement:
    'for' identifier 'in' 'reverse'? expression '..' expression 'loop'
        statements
    'end' 'loop' ';'
;

// Volání procedury/funkce
procedureCall: identifier actualParameters? ';';
actualParameters: '(' expressionList ')';
expressionList: expression (',' expression)*;

// Return příkaz
returnStatement: 'return' expression? ';';

// Exit příkaz
exitStatement: 'exit' identifier? ';';

// Výrazy
expression: simpleExpression (relationalOperator simpleExpression)?;
relationalOperator: '=' | '/=' | '<' | '<=' | '>' | '>=';
simpleExpression: sign? term (addingOperator term)*;
sign: '+' | '-';
addingOperator: '+' | '-' | 'or';
term: factor (multiplyingOperator factor)*;
multiplyingOperator: '*' | '/' | 'mod' | 'and';
factor: primary ('**' primary)?;

primary:
    integerLiteral
    | realLiteral
    | stringLiteral
    | functionCall
    | variable
    | '(' expression ')'
    | 'not' primary
;

functionCall: identifier actualParameters;

// Literály a identifikátory
integerLiteral: INTEGER;
realLiteral: REAL;
stringLiteral: STRING;
identifier: IDENTIFIER;

// Lexer pravidla
INTEGER: [0-9]+;
REAL: [0-9]+ '.' [0-9]+ ([eE][+-]?[0-9]+)?;
STRING: '"' ~["]* '"';
IDENTIFIER: [a-zA-Z][a-zA-Z0-9_]*;

// Whitespace a komentáře
WS: [ \t\r\n]+ -> skip;
COMMENT: '--' ~[\r\n]* -> skip;