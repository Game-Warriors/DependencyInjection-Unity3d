  version : 0.8.8
  feature : implementing transient object resolving in singleton objects constructor, properties and initialization method.

  version : 0.8.7
  Fix compatibility to old unity dotnet

  version : 0.8.6
  Update readme and add none generic method to service collections

  version : 0.8.5
  Implement new lazy loading pipeline

  version : 0.8.4
  Remove inject attribute and check exception handling

  version : 0.8.3
  Add extra loading in build pipeline

  version : 0.8.2
  Add circle dependency detection in constructor to service collection build pipeline

  version : 0.8.1
  Add IServiceProvider object as argument to startup method callback

  version : 0.8.0
  Improve service collection structure, fix MonoBehaviour transient bug and add factory method to transient instantiation.

  version : 0.7.0
  Implementation new instantiating transient class from DI for ServiceCollectionEnumerator.

  version : 0.6.0
  Initial basic implementation of instantiating transient class from DI.
  
  version : 0.5.0
  Add Coroutine base service collection named ServiceCollectionEnumerator which use no references to task class or threading namespace in compare to ServiceCollection class.

  version : 0.4.0
  Change the service locator inside service provider class from static to instance object class

  version : 0.3.0
  Add method call after wait for loading step in service collection. The method name can be customize and can has optional parameter for injection

  version : 0.2.0
  Change and improvement on service collection loading pipeline, Inject properties now has value inside WaitForLoading() method