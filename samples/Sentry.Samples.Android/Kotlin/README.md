# Compiling Kotlin

To compile, install Kotlin:

`sdkman`: `sdk install kotlin`

For [more options see Kotlin docs](https://kotlinlang.org/docs/command-line.html#create-and-run-an-application).

Creates the `jar` file by compiling the Kotlin source:

```kotlin
kotlinc buggy.kt -include-runtime -d buggy.jar
```