# Unity of Bugs

A set of different programming errors that would cause errors on a real world game.  
This serves as a test project for crash reporting tools.

## Current cases

* Assert False - only relevant on the editor (unless `BuildOptions.ForceEnableAssertions=true`).
* C# throw null - unhandled by user code.
* C# throw/try/catch and `Debug.LogException`.
* C# Log an instance of an Exception as a String - The raw `Exception.ToString()`.
* C# Unity `Log.Debug` calls.
* Android: Kotlin `throw Exception` - unhandled by user code.
* Android: Kotlin `throw` on a background thread - **Crashes the app**.
* Android: C bad access - Requires IL2CPP. **Crashes the app**.

Currently the native plugins are focused on Android. But iOS will come next, and standalone (desktop) after that.

## Other cases

`UnityEngine.Diagnostics.Utils` has a method called `ForceCrash()`.  
It takes a `ForcedCrashCategory` as an argument [which includes 4 different types of errors](https://docs.unity3d.com/2019.1/Documentation/ScriptReference/Diagnostics.ForcedCrashCategory.html).  
These will be added too in the future. Quicker if you'd like to contribute with a PR :)

## Messages breakdown

`Application.logMessageReceived` is an entry point for all messages. It has the following delegate

```csharp
public delegate void LogCallback(string condition, string stackTrace, LogType type);
```

There are examples of `Unity` calls and outputs:

### Single lines

* `Debug.Log("Log from Unity!");`
  * `Condition` - Log from Unity!
  * `StackTrace`
  ```
  UnityEngine.Debug:Log(Object)
  BugFarm:ThrowExceptionAndCatch() (at Assets/Scripts/BugFarm.cs:38)
  UnityEngine.EventSystems.EventSystem:Update() (at C:/Program Files/Unity/Hub/Editor/2019.4.21f1/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/EventSystem/EventSystem.cs:377)
  ```
  * `LogType` - `Log`
* `Debug.LogFormat("Log from {0}!", "Unity");`
  * `Condition` - Log from Unity!
  * `StackTrace`
  ```
  UnityEngine.Debug:LogFormat(Object)
  BugFarm:ThrowExceptionAndCatch() (at Assets/Scripts/BugFarm.cs:38)
  UnityEngine.EventSystems.EventSystem:Update() (at C:/Program Files/Unity/Hub/Editor/2019.4.21f1/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/EventSystem/EventSystem.cs:377)
  ```
  * `LogType` - `Log`
* `Debug.LogWarning("Warning from Unity!");`
  * `Condition` - Warning from Unity!
  * `StackTrace`
  ```
  UnityEngine.Debug:LogWarning(Object)
  BugFarm:ThrowExceptionAndCatch() (at Assets/Scripts/BugFarm.cs:38)
  UnityEngine.EventSystems.EventSystem:Update() (at C:/Program Files/Unity/Hub/Editor/2019.4.21f1/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/EventSystem/EventSystem.cs:377)
  ```
  * `LogType` - `Warning`
* `Debug.LogWarningFormat("Warning from {0}!", "Unity");`
  * `Condition` - Warning from Unity!
  * `StackTrace`
  ```
  UnityEngine.Debug:LogWarningFormat(Object)
  BugFarm:ThrowExceptionAndCatch() (at Assets/Scripts/BugFarm.cs:38)
  UnityEngine.EventSystems.EventSystem:Update() (at C:/Program Files/Unity/Hub/Editor/2019.4.21f1/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/EventSystem/EventSystem.cs:377)
  ```
  * `LogType` - `Warning`
* `Debug.LogError("Error from Unity!");`
  * `Condition` - Error from Unity!
  * `StackTrace`
  ```
  UnityEngine.Debug:LogError(Object)
  BugFarm:ThrowExceptionAndCatch() (at Assets/Scripts/BugFarm.cs:38)
  UnityEngine.EventSystems.EventSystem:Update() (at C:/Program Files/Unity/Hub/Editor/2019.4.21f1/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/EventSystem/EventSystem.cs:377)
  ```
  * `LogType` - `Error`
* `Debug.LogErrorFormat("Error from {0}!", "Unity");`
  * `Condition` - Error from Unity!
  * `StackTrace`
  ```
  UnityEngine.Debug:LogErrorFormat(Object)
  BugFarm:ThrowExceptionAndCatch() (at Assets/Scripts/BugFarm.cs:38)
  UnityEngine.EventSystems.EventSystem:Update() (at C:/Program Files/Unity/Hub/Editor/2019.4.21f1/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/EventSystem/EventSystem.cs:377)
  ```
  * `LogType` - `Error`
* `Debug.LogAssertion("Assertion from Unity!");`
  * `Condition` - Assertion from Unity!
  * `StackTrace`
  ```
  UnityEngine.Debug:LogAssertion(Object)
  BugFarm:ThrowExceptionAndCatch() (at Assets/Scripts/BugFarm.cs:38)
  UnityEngine.EventSystems.EventSystem:Update() (at C:/Program Files/Unity/Hub/Editor/2019.4.21f1/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/EventSystem/EventSystem.cs:377)
  ```
  * `LogType` - `Assert`
* `Debug.LogAssertionFormat("Assertion from {0}!", "Unity");`
  * `Condition` - Assertion from Unity!
  * `StackTrace`
  ```
  UnityEngine.Debug:LogAssertionFormat(Object)
  BugFarm:ThrowExceptionAndCatch() (at Assets/Scripts/BugFarm.cs:38)
  UnityEngine.EventSystems.EventSystem:Update() (at C:/Program Files/Unity/Hub/Editor/2019.4.21f1/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/EventSystem/EventSystem.cs:377)
  ```
  * `LogType` - `Assert`
* `Debug.LogException(new Exception("Just an exception!"));`
  * `Condition` - Exception: Just an exception!
  * `StackTrace`
  ```
  UnityEngine.Debug:LogException(Exception)
  BugFarm:ThrowExceptionAndCatch() (at Assets/Scripts/BugFarm.cs:38)
  UnityEngine.EventSystems.EventSystem:Update() (at C:/Program Files/Unity/Hub/Editor/2019.4.21f1/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/EventSystem/EventSystem.cs:377)
  ```
  * `LogType` - `Exception`
* `throw new Exception("Just an exception!");`
  * `Condition` - Exception: Just an exception!
  * `StackTrace`
    ```
    BugFarm.ThrowExceptionAndCatch () (at Assets/Scripts/BugFarm.cs:42)
    UnityEngine.Events.InvokableCall.Invoke () (at <17ad9609ae064f2c9315931ff97adcf1>:0)
    UnityEngine.Events.UnityEvent.Invoke () (at <17ad9609ae064f2c9315931ff97adcf1>:0)
    UnityEngine.UI.Button.Press () (at C:/Program Files/Unity/Hub/Editor/2019.4.21f1/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/UI/Core/Button.cs:68)
    UnityEngine.UI.Button.OnPointerClick (UnityEngine.EventSystems.PointerEventData eventData) (at C:/Program Files/Unity/Hub/Editor/2019.4.21f1/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/UI/Core/Button.cs:110)
    UnityEngine.EventSystems.ExecuteEvents.Execute (UnityEngine.EventSystems.IPointerClickHandler handler, UnityEngine.EventSystems.BaseEventData eventData) (at C:/Program Files/Unity/Hub/Editor/2019.4.21f1/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/EventSystem/ExecuteEvents.cs:50)
    UnityEngine.EventSystems.ExecuteEvents.Execute[T] (UnityEngine.GameObject target, UnityEngine.EventSystems.BaseEventData eventData, UnityEngine.EventSystems.ExecuteEvents+EventFunction`1[T1] functor) (at C:/Program Files/Unity/Hub/Editor/2019.4.21f1/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/EventSystem/ExecuteEvents.cs:261)
    UnityEngine.EventSystems.EventSystem:Update() (at C:/Program Files/Unity/Hub/Editor/2019.4.21f1/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/EventSystem/EventSystem.cs:377)
    ```
  * `LogType` - `Exception`
* `throw new CustomException("Custom bugs are here!");`
  * `Condition` - CustomException: Custom bugs are here!
  * `StackTrace`
    ```
    BugFarm.ThrowExceptionAndCatch () (at Assets/Scripts/BugFarm.cs:43)
    UnityEngine.Events.InvokableCall.Invoke () (at <17ad9609ae064f2c9315931ff97adcf1>:0)
    UnityEngine.Events.UnityEvent.Invoke () (at <17ad9609ae064f2c9315931ff97adcf1>:0)
    UnityEngine.UI.Button.Press () (at C:/Program Files/Unity/Hub/Editor/2019.4.21f1/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/UI/Core/Button.cs:68)
    UnityEngine.UI.Button.OnPointerClick (UnityEngine.EventSystems.PointerEventData eventData) (at C:/Program Files/Unity/Hub/Editor/2019.4.21f1/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/UI/Core/Button.cs:110)
    UnityEngine.EventSystems.ExecuteEvents.Execute (UnityEngine.EventSystems.IPointerClickHandler handler, UnityEngine.EventSystems.BaseEventData eventData) (at C:/Program Files/Unity/Hub/Editor/2019.4.21f1/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/EventSystem/ExecuteEvents.cs:50)
    UnityEngine.EventSystems.ExecuteEvents.Execute[T] (UnityEngine.GameObject target, UnityEngine.EventSystems.BaseEventData eventData, UnityEngine.EventSystems.ExecuteEvents+EventFunction`1[T1] functor) (at C:/Program Files/Unity/Hub/Editor/2019.4.21f1/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/EventSystem/ExecuteEvents.cs:261)
    UnityEngine.EventSystems.EventSystem:Update() (at C:/Program Files/Unity/Hub/Editor/2019.4.21f1/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/EventSystem/EventSystem.cs:377)
    ```
  * `LogType` - `Exception`
  
### Multiple lines

**Note:** for `try\catch` clause, if exception is thrown in `try`, only line from `catch` is generated.

**Note:** for `try\catch\finally` clause, line from `catch` and `finally` is generated.

* try\catch `Exception` with `Debug.LogException`
  ```csharp
  try
  {
      throw new Exception("Just an exception!");
  }
  catch (Exception e)
  {
      Debug.LogException(e);
  }
  ```
  * `Condition` - Exception: Just an exception!
  * `StackTrace`
    ```
    BugFarm.ThrowExceptionAndCatch () (at Assets/Scripts/BugFarm.cs:41)
    UnityEngine.Debug:LogException(Exception)
    BugFarm:ThrowExceptionAndCatch() (at Assets/Scripts/BugFarm.cs:45)
    UnityEngine.EventSystems.EventSystem:Update() (at C:/Program Files/Unity/Hub/Editor/2019.4.21f1/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/EventSystem/EventSystem.cs:377)
    ```
  * `LogType` - `Exception`
* try\catch `CustomException` with `Debug.LogException`
  ```csharp
  try
  {
      throw new CustomException("Custom bugs are here!");
  }
  catch (Exception e)
  {
      Debug.LogException(e);
  }
  ```
  * `Condition` - CustomException: Custom bugs are here!
  * `StackTrace`
    ```
    BugFarm.ThrowExceptionAndCatch () (at Assets/Scripts/BugFarm.cs:41)
    UnityEngine.Debug:LogException(Exception)
    BugFarm:ThrowExceptionAndCatch() (at Assets/Scripts/BugFarm.cs:45)
    UnityEngine.EventSystems.EventSystem:Update() (at C:/Program Files/Unity/Hub/Editor/2019.4.21f1/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/EventSystem/EventSystem.cs:377)
    ```
  * `LogType` - `Exception`
* try\catch `Exception` with `Debug.Log`
  ```csharp
  try
  {
      throw new Exception("Just an exception!");
  }
  catch (Exception e)
  {
      Debug.Log(e);
  }
  ```
  * `Condition` - System.Exception: Just an exception!
  at BugFarm.ThrowExceptionAndCatch () [0x00002] in C:\Projects\Unity\sentry-unity\samples\unity-of-bugs\Assets\Scripts\BugFarm.cs:43 
  * `StackTrace`
    ```
    UnityEngine.Debug:Log(Object)
    BugFarm:ThrowExceptionAndCatch() (at Assets/Scripts/BugFarm.cs:47)
    UnityEngine.EventSystems.EventSystem:Update() (at C:/Program Files/Unity/Hub/Editor/2019.4.21f1/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/EventSystem/EventSystem.cs:377)
    ```
  * `LogType` - `Log`
* try\catch\finally `Exception`
  ```csharp
  try
  {
      throw new Exception("Just an exception!");
  }
  catch (Exception e)
  {
      Debug.LogException(e);
  }
  finally
  {
      Debug.Log("[finally] log!");
  }
  ```
  * `catch`
    * `Condition` - Exception: Just an exception!
    * `StackTrace`
      ```
      BugFarm.ThrowExceptionAndCatch () (at Assets/Scripts/BugFarm.cs:43)
      UnityEngine.Debug:LogException(Exception)
      BugFarm:ThrowExceptionAndCatch() (at Assets/Scripts/BugFarm.cs:47)
      UnityEngine.EventSystems.EventSystem:Update() (at C:/Program Files/Unity/Hub/Editor/2019.4.21f1/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/EventSystem/EventSystem.cs:377)
      ```
    * `LogType` - `Exception`
  * `finally`
    * `Condition` - [finally] log!
    * `StackTrace`
      ```
      UnityEngine.Debug:Log(Object)
      BugFarm:ThrowExceptionAndCatch() (at Assets/Scripts/BugFarm.cs:51)
      UnityEngine.EventSystems.EventSystem:Update() (at C:/Program Files/Unity/Hub/Editor/2019.4.21f1/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/EventSystem/EventSystem.cs:377)
      ```
    * `LogType` - `Log`
