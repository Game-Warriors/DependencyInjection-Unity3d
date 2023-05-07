# DependencyInjection
![GitHub](https://img.shields.io/github/license/svermeulen/Extenject)

## Table Of Contents

<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
<summory>

  - [Introduction](#introduction)
  - [Features](#features)
  - [Installation](#installation)
  - [Startup](#startup)
  - [How To Use](#how-to-use)
</summory>

## Introduction
This is the high performance, lightweight and simple to use for Dependency injection library, implemented in C# language for using in Unity3D engine but it’s can be use in other .net environments. This library design to setup and startup program pipeline by creating and injecting dependency into default classes by constructor and method injection call(by specific method name) for MonoBehaviour classes.

Support platforms: 
* PC/Mac/Linux
* iOS
* Android
* WebGL
* UWP App

```text
* Note: it's may work on other platforms,the source code just used C# code and .net standard version 2.0.
```

This library package just provide Dependency Injection for other requier feature, the other repo package can be use.
* For update method and other timing operation feature the [Task Domain](https://github.com/Game-Warriors/TaskDomain-Unity3d) can be use

```
```
This library used in following games:
</br>
[Street Cafe](https://play.google.com/store/apps/details?id=com.aredstudio.streetcafe.food.cooking.tycoon.restaurant.idle.game.simulation)
</br>
[Idle Family Adventure](https://play.google.com/store/apps/details?id=com.aredstudio.idle.merge.farm.game.idlefarmadventure)


## Features
* Supports both normal C# classes and MonoBehaviours(instantiate automaticly)
* Constructor injection
* Specify one Initialization method injection
* Property injection
* Lazy loading
* Binding by service loactore class

## Installation
This library can be added by unity package manager form git repository or could be downloaded and directly add to project.

## Startup
The dependency injection system better to setup and startup in Awake() method of a MonoBehaviour script. The recommendation is to create startup class and add the script to a game object in start scene. it's better to avoid using common unity startup pipeline like Start, OnEnable callback methods to managing code execution order after collection build ends, and use library features to start logics pipelines.

The library provide the singleton and transient lifetime for the objects. the classes could add by derived abstractions or direct object. The singleton classes could has multiply interface and all related to single instance. The more description about practical usages.

* Singletons : the singleton objects could register in DI by passing instance or let the service collection instantiate the object. The creation pipeline of singleton class starts after calling build method of service collection. in first step call the constructor calls for non MonoBehaviour  classes after all classes has been constructed, service call WaitForLoading() method and finally after all loading accomplished, the service call inject method which has specified by the name in service collection constructor. the singleton object can be injected into transient classes and the DI will pass the singleton reference.

* Transients: the transient object could register in DI by factor method or let the default service factory instantiate the object. In the custom method factory approach the service provider automatically fill the inject properties. the transient objects can be injected into singleton classes and the DI will create new instance for every instance requests.

## How To Use
1.  __Constructing Service Collection__
    * There are two service collection classes in package which is ServiceCollection and ServiceCollectionEnumerator, although those are very similar in scheme and usage, but the difference is, the service collection use Task and Threading library meanwhile the ServiceCollectionEnumerator use IEnumerator interface and unity3D coroutine feature.
    * There is capability to register one object by many abstractions.
```csharp
    serviceCollection.AddSingleton<IContentDatabase, ResourceSystem>();
    serviceCollection.AddSingleton<ISpriteDatabase, ResourceSystem>();
```

* The constructure could fill with custom service provider, initialization method name which describe later and extra loading call back.
    * Service provider: the service container class which could construct before service collection creation and fill some object or it can be ignore to service collection handler the provider.

    * Initialization method name: each object that add to service collection can has the method by specific name and the has arguments which exist in service container. The service collection finds the method by reflection library and fill all the object arguments if those exist in service container.

    * Extra loading: The extra loading method call after all objects loading has done. it’s because maybe loading some data depends on loading of other objects and this method trigger after all resources loading has been done.

```csharp
public ServiceCollection(string initMethodName = default, Func<Task> extraLoading = default) 
  : this(new ServiceProvider(), initMethodName, extraLoading)
{
}
```

2.  __Adding Services__
* <b>Adding Singleton Service</b>
: The singleton classes can add by different overload of the method “AddSingleton“ which has:
    * generic or non-generic methods: The target class type can add by generic type or non-generic type by passing class type by argument. The class type can be simple concrete classes or couple by abstraction.
    * by instance or non-instance objects: by default, the service container handle initializing in building collection pipeline although there is a feature to pass the created default instance into collection. in both usages the service collection applies property filling and calling initialization method routine.
    * by has loading method: if the object needs lazy loading during the setup process, there is overload for registration which passed the object instance as input and there is possibility to select a method inside the object instance for lazy loading.

```csharp
private async void Awake()
{
    ServiceCollection serviceCollection 
      = new ServiceCollection(initMethodName: nameof(Initialization));
    serviceCollection.AddSingleton<GameManagerSample>(this, input => input.WaitForLoading());
    serviceCollection.AddSingleton<IAnalyticConfig, GameManagerSample>(this);
    serviceCollection.AddSingleton<IResourceConfig, GameManagerSample>(this);
    serviceCollection.AddSingleton<IPool, PoolSystem>(input => input.WaitForLoading());
    serviceCollection.AddSingleton<IBehaviorInitializer<string>, CompondInitializer>();
    serviceCollection.AddSingleton<IVariableDatabase, ResourceSystem>(input => input.WaitForLoading());
    serviceCollection.AddSingleton<IContentDatabase, ResourceSystem>();
    serviceCollection.AddSingleton<ISpriteDatabase, ResourceSystem>();
    await serviceCollection.Build(Startup);
}
```


* <b>Adding Transient Service</b>
: The transient classes can add by different of overload of the method “AddTransient“ which has:
    * Simple no parameter method: The service container handles the instantiating object and filling property routine.
    * Factory input method argument: The service container fetch object instance from the factory method and by considering the optional “isFillProperties” parameter do filling properties or not.
    * generic or non-generic methods: The target class type can add by generic type or non-generic type by passing class type by argument. The class type can be simple concrete classes or couple by abstraction.

```csharp
private async void Awake()
{
    ServiceCollection serviceCollection 
      = new ServiceCollection(initMethodName: nameof(Initialization));
    serviceCollection.AddTransient<IBoard, BoardItem>();
    serviceCollection.AddTransient<ICurrnecy, WoodCurrency>((serviceProvider) 
      => { return new WoodCurrency(serviceProvider); });
    await serviceCollection.Build(Startup);
}
```

3.  __Build The Collection__
After registering all require services into service collection, it’s having to call the build method to start build service container pipeline. for ServiceCollection the build method return Task and for the  ServiceCollectionEnumerator  the build method return IEnumerator. like following examples. The build method could fill by method callback which call after pipeline ends successfully.

```csharp
private async void Awake()
{      
    ServiceCollection serviceCollection = 
      new ServiceCollection(initMethodName: nameof(Initialization));   
    //Register the services here   
    await serviceCollection.Build(Startup);
}

private void Startup(IServiceProvider serviceProvider)
{
}
```

```text
* Note: It’s better to await for build task in order to unity main thread can catch the possible exception.
```

```csharp
private async void Awake()
{
    ServiceCollectionEnumerator serviceCollection 
      = new ServiceCollectionEnumerator(nameof(Initialization));
    //Register the services here
    StartCoroutine(serviceCollection.Build(Startup));
}

private void Startup(IServiceProvider serviceProvider)
{
}
```
Inside of build method, the pipeline logic progress for objects as following description order:
* Instantiating objects and call the constructor method by filling the registered objects as input argument for constructor by the exist abstraction inside the service container.

```text
* Note: to avoiding the circle references between instantiating objects and the requested service as agreement in constructor.
```
```text
* Note: it’s better to mention [UnityEngine.Scripting.Preserve] attribute for constructors to prevent the compiler to possible striping code.
```

* Find the private properties by get and set capability and filling by the exist abstraction inside the service container.
```text
* Note: it’s better to mention [UnityEngine.Scripting.Preserve] attribute for properties to prevent the compiler to possible striping code.
```
* Calling passed loading methods and wait till all loadings done, concurrently. the calls will do on unity main thread although the inside loading method could be new Task or thread running.
```text
* Note: The loading method is just singleton classes which instantiating in build collection pipeline.
```
* Calling the extra loading method and wait for it return Task till it’s done. the extra loading method has been passed in collection constructor method.

* Calling the implemented method inside the registered objects in collection by specify name which passed in the service collection constructor. The service collection fetches the method arguments and fill the parameters by registered object which exist in the collection.

* The procedure comes to end and the collection call the done callback if the callback passed as build method argument.

</br>
The example project
</br>

[Example Project](https://github.com/Game-Warriors/TemplateProject)