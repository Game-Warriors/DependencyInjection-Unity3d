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
This is high-performance, lightweight, and simple to use for Dependency injection library, implemented in C# language for use in Unity3D engine but it can be used in other .net environments. This library is designed to set up and start-up program pipeline by creating and injecting dependency into default classes by constructor and method injection call(by specific method name) for MonoBehaviour classes.

Support platforms: 
* PC/Mac/Linux
* iOS
* Android
* WebGL
* UWP App

```text
* Note: it's may work on other platforms, the source code just used C# code and .net standard version 2.0.
```

This library package just provides Dependency Injection for other required features, the other repo package can be used.
* For the update method and other timing operation features the [Task Domain](https://github.com/Game-Warriors/TaskDomain-Unity3d) can be used

```
```
This library used in the following games:
</br>
[Street Cafe](https://play.google.com/store/apps/details?id=com.aredstudio.streetcafe.food.cooking.tycoon.restaurant.idle.game.simulation)
</br>
[Idle Family Adventure](https://play.google.com/store/apps/details?id=com.aredstudio.idle.merge.farm.game.idlefarmadventure)


## Features
* Supports both normal C# classes and MonoBehaviours(instantiate automatically)
* Constructor injection
* Specify one Initialization method injection
* Property injection
* Lazy loading
* Binding by service locator class

## Installation
This library can be added by the Unity3D package manager from the git repository or could be downloaded and directly add to the project.
for more information about how to install a package by unity package manager, please read the manual in this link:
[Install a package from a Git URL](https://docs.unity3d.com/Manual/upm-ui-giturl.html)

## Startup
The dependency injection system is better to set up and start-up in the Awake() method of a MonoBehaviour script. The recommendation is to create a 'Startup' class and add the script to a game object in the start scene. it's better to avoid using common unity startup pipelines like Start and OnEnable callback methods to manage code execution order after the collection build ends and use library features to start logics pipelines.

The library provides the singleton and transient lifetime for the objects. the classes could add by derived abstractions or direct objects. The singleton classes could have multiple interfaces and all are related to a single instance. The more description about practical usages.

* Singletons: the singleton objects could register in DI by passing instances or letting the service collection instantiate the object. The creation pipeline of the singleton class starts after calling the build method of service collection. in the first step call the constructor calls for non-MonoBehaviour classes after all classes have been constructed, the service call WaitForLoading() method, and finally after all loading is accomplished, the service call injects method which has specified by the name in the service collection constructor. the singleton object can be injected into transient classes and the DI will pass the singleton reference.

* Transients: the transient object could register in DI by factor method or let the default service factory instantiate the object. In the custom method factory approach, the service provider automatically fills the inject properties. the transient objects can be injected into singleton classes and the DI will create a new instance for every instance request.

## How To Use
1.  __Constructing Service Collection__
    * There are two service collection classes in the package which are ServiceCollection and ServiceCollectionEnumerator, although those are very similar in scheme and usage, the difference is, the service collection uses Task and Threading library meanwhile the ServiceCollectionEnumerator uses the IEnumerator interface and unity3D coroutine feature.
    * There is the capability to register one object by many abstractions.
```csharp
    serviceCollection.AddSingleton<IContentDatabase, ResourceSystem>();
    serviceCollection.AddSingleton<ISpriteDatabase, ResourceSystem>();
```

* The constructor could fill with a custom service provider, initialization method name which describe later, and extra loading call back.
    * Service provider: the service container class which could construct before service collection creation and fill some object or it can be ignored by the service collection handler the provider.

    * Initialization method name: each object that adds to the service collection can have the method by a specific name and has arguments that exist in the service container. The service collection finds the method by reflection library and fills all the object arguments if those exist in the service container.

    * Extra loading: The extra loading method call after all object loading has been done. it’s because maybe loading some data depends on the loading of other objects and this method triggers after all resource loading has been done.

```csharp
public ServiceCollection(string initMethodName = default, Func<Task> extraLoading = default) 
  : this(new ServiceProvider(), initMethodName, extraLoading)
{
}
```

2.  __Adding Services__
* <b>Adding Singleton Service</b>
: The singleton classes can add by a different overload of the method “AddSingleton“ which has:
    * generic or non-generic methods: The target class type can add by generic type or non-generic type by passing the class type by argument. The class type can be simple concrete classes or coupled by abstraction.
    * by instance or non-instance objects: by default, the service container handle initializing in building collection pipeline although there is a feature to pass the created default instance into the collection. in both usages, the service collection applies properly filling and calling initialization method routine.
    * by has loading method: if the object needs lazy loading during the setup process, there is overload for registration which passed the object instance as input and there is a possibility to select a method inside the object instance for lazy loading.

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
: The transient classes can add by different overloads of the method “AddTransient“ which has:
    * Simple no parameter method: The service container handles the instantiating object and filling property routinely.
    * Factory input method argument: The service container fetches an object instance from the factory method by considering whether the optional “isFillProperties” parameter does filling properties or not.
    * generic or non-generic methods: The target class type can add by generic type or non-generic type by passing the class type by argument. The class type can be simple concrete classes or coupled by abstraction.

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
After registering all required services into service collection, it’s having to call the build method to start building the service container pipeline. for ServiceCollection the build method return Task and for the  ServiceCollectionEnumerator the build method returns IEnumerator. like the following examples. The build method could fill by method callback which calls after the pipeline ends successfully.

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
* Note: It’s better to await for the build task to unity main thread can catch the possible exception.
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
Inside of build method, the pipeline logic progress for objects in the following description order:
* Instantiating objects and calling the constructor method by filling the registered objects as input arguments for the constructor by the existing abstraction inside the service container.

```text
* Note: to avoid the circle references between instantiating objects and the requested service as the argument in the constructor.
```
```text
* Note: it’s better to mention [UnityEngine.Scripting.Preserve] attribute for properties to prevent the compiler from possibly striping code.
```

* Find the private properties by getting and setting capability and filling by the existing abstraction inside the service container.
```text
* Note: it’s better to mention [UnityEngine.Scripting.Preserve] attribute for properties to prevent the compiler to possible striping code.
```
* Calling passed loading methods and waiting till all loadings are done, concurrently. the calls will do on the unity main thread although the inside loading method could be a new Task or thread running.
```text
* Note: The loading method is just singleton classes that instantiate in the build collection pipeline.
```
* Calling the extra loading method and wait for it to return Task till it’s done. the extra loading method has been passed in the collection constructor method.

* Calling the implemented method inside the registered objects in the collection by specifying the name which passed in the service collection constructor. The service collection fetches the method arguments and fills the parameters by registered objects which exist in the collection.

* The procedure comes to an end and the collection calls the done callback if the callback passed as a build method argument.

</br>
The example project
</br>

[Example Project](https://github.com/Game-Warriors/TemplateProject)