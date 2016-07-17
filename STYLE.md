# ETGMod code style guide v1.0

### Preamble

The purpose of this guide is to aid in the understandability of code by all contributors.  
By making sure that everyone follows the same rules, we can ensure that two APIs will not look completely different.  
Please read the entirety of this document before submitting code.

### Indentation

ETGMod source code uses spaces for indentation. Please set the tab size to 4 spaces in your IDE/editor.

### Case

All types should use Pascal case, e.g. `Object` or `Vector3`.  
Class fields use Pascal case: `ThisIsAFieldName`. **Note:** `thisIsAFieldName` is camelCase, *not* Pascal case.  
Methods should use Pascal case, e.g. `Update` or `Awake`.  
Method arguments should use Camel case, e.g. `cachedPath`.  
Namespaces use Pascal case.  
Properties use Pascal case.

### Properties

It's recommended not to create properties that have a similiar name to the underlying field, but if such usage is needed, prefix the field with an underscore (`_`).

### Operators

When using operators like `+` or `=` put a space before and after the operator (`1 + 1` instead of `1+1`).  
The increment and decrement operators are an exception to this rule (use `variable++;`, not `variable ++ ;`).

### Patch classes

Please put these two lines:

```
#pragma warning disable 0626
#pragma warning disable 0649
```

at the beginning of every single file **that directly patches the game code** - it'll disable two redundant warnings that only clutter the build log.

### Cross-Platformability

ETGMod is and will always be a cross platform modding framework.
Cross platform means here the support of the 3 desktop operating systems that Enter the Gungeon is released on, not counting PS4.

Due to the nature of the method we use to mod the game and weird platform-dependencies, some deobfuscated classes differ in name on different platforms.  
For this the Cross utility was created. You can see an example of it's usage in [`Player.cs`](Assembly-CSharp.Base.mm/src/Core/Player.cs).

Do not implement your own cross platform helpers or hacks.

---

Note: This document may (and probably will) be changed. Make sure to check every once in a while to see if something's been changed.  
The version number will be modified if so.
