Simple Roslyn-based extension to simplify work with NSubstitute. Inspired by https://github.com/ycherkes/MockIt

At the moment the only supported feature is mocks generation from unfilled constructors "new MyClass()". Notes:
- Works only if NSubstitute library is referenced in project
- Mocks are generated for interfaces and abstract classes only, for arguments "TODOs" are left
- Opinionated naming schema is used for generated fields - e.g. \_myField