Simple Roslyn-based extension to speed up tests development with NSubstitute mocking library. Inspired by https://github.com/ycherkes/MockIt

## Supported features:
* At the moment the only supported feature is mocks generation from constructors "new MyClass()". 

## How to use:
* Install as Visual Studio extension
* Refer NSubstitute library from your test project (analyzer ignores code if NSubstitute is not references)
* Type code for creating and instance of class you want to test, but do not specify any constructor arguments - e.g. ```var sut = new MyServce()```
* Visual Studio will highlight empty constructor and will offer a quick fix (light bulb) to generate mocks
* Once you select quick fix extension will automatically generate code:
  * Interface mocks as fields - e.g. ```private IAnotherService _anotherServiceMock```
  * Mocks initializers - e.g. ```_anotherServiceMock = Substitute.For<IAnotherService>();```
  * Constructor arguments - e.g. ```new MyService(_anotherServiceMock, default(string), default(int))```
* If you will add one more parameters into constructor then extension will offer you to generate mocks for this parameter