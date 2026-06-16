
- [x] Language version: Extension indexers require preview/next language version (C# 14 rejects, Next/Preview accepts)
  - [x] At declaration
  - [x] At consumption

- [x] Parsing of indexers in extension blocks

- [ ] Declaration tests
  - [x] Basic declarations tests for features that ordinary indexers support today 
  - [x] Receiver must be named
  - [x] The following modifiers are not allowed:
    - [x] `abstract`
    - [x] `virtual`
    - [x] `override`
    - [x] `new`
    - [x] `sealed`
    - [x] `partial`
    - [x] `protected` (or any of the related accessibility modifiers)
    - [x] static
  - [x] Init accessors are not allowed
  - [ ] No implicit `this` accessor in the body     
  - [x] IndexerName attribute
      - Source/metadat 
      - Single indexer
      - Multiple indexers with matching and different attribute values     

- [ ] Consumption
  - [x] Basic consumption scenarios
  - [ ] Not supported on `base` as a receiver
  - [ ] Not supported on a type as a receiver
  - [x] Lookup/overload resolution priorities
    - Instance indexers come first
    - Then instance based impleicit indexers
    - Then extension explicit indexers
    - Then extension implicit indexers
  - [x] Extension scoping rules
  - [x] Various ambiguity scenarios
  - [x] Type inference
  - [x] Implicit indexers
    - [x] Various permutations of Count/Length
    - [x] Arrays/string scenarios
  - [x] Assignment scenarios including increment, compound assignment, deconstruction assignment, etc.  
  - [x] Order of evaluation for read/write scenarios
    - [x] Receiver is a class
    - [x] Receiver is a struct
    - [x] Receiver is an LValue
    - [x] Receiver is an RVAlue
  - [x] Other implicit element-access forms
    - [x] List patterns
    - [x] Object initializers
    - [x] Spread elements
  - [x] String interpolation handlers
  - [x] Params
  - [x] Disallowed in Expression Trees
  - [x] Dynamic arguments are disallowed
  - [x] In `nameof`
  - [x] Consumption of implementation methods for accessors
     
- [x] Nullable analysis

- [x] Ref safety analysis

- [ ] Analyzer Actions for extension.
- [ ] SemanticModel APIs
  - [ ] Lookup related as well
- [x] IOperation/Flow graph
- [ ] Symbol display

- [x] Cref in XML doc comments
  - Indexer
  - Accessor implementation methods 

- [ ] Unsafe evolution (extension indexers marked as RequiresUnsafe)

- [x] Metadata
  - [x] Production
  - [x] Consumption

- [ ] Interoperability with VB
  - Consumption of implementation methods
     
- [ ] Public API

## Test Categories

### 1. Declaration Tests

#### 1.1 Basic Declaration

- [ ] Extension indexer with get accessor only
- [ ] Extension indexer with set accessor only  
- [ ] Extension indexer with both get and set accessors
- [ ] Extension indexer with no accessors (error)
- [ ] Extension indexer with expression-bodied get accessor
- [ ] Extension indexer with expression-bodied set accessor
- [ ] Extension indexer in generic extension block
- [ ] Extension indexer in non-generic extension block
- [ ] Multiple extension indexers in same extension block with different signatures
- [ ] Extension indexer with different parameter types (int, string, custom types)
- [ ] Extension indexer with multiple parameters

#### 1.2 Named Receiver Parameter

- [ ] Extension indexer with named receiver parameter (valid)
- [ ] Extension indexer without named receiver parameter (error - must be rejected)
- [ ] Extension indexer with multiple receiver parameters (error)
- [ ] Extension indexer with `this` receiver parameter name
- [ ] Extension indexer with underscore `_` receiver discard

#### 1.3 Advanced Features

- [ ] Extension indexer with ref-returning get accessor
- [ ] Extension indexer with ref readonly-returning get accessor
- [ ] Extension indexer with `scoped` parameters
- [ ] Extension indexer with `params` parameter (get-only)
- [ ] Extension indexer with `params` parameter (both get and set)
- [ ] Extension indexer with optional parameters
- [ ] Extension indexer with default parameter values
- [ ] Extension indexer with attributes on accessors
- [ ] Extension indexer with attributes on parameters
- [ ] Extension indexer with attributes on the indexer itself

#### 1.4 Restricted Modifiers (All Should Error)

- [ ] Extension indexer with `abstract` modifier (error)
- [ ] Extension indexer with `virtual` modifier (error)
- [ ] Extension indexer with `override` modifier (error)
- [ ] Extension indexer with `new` modifier (error)
- [ ] Extension indexer with `sealed` modifier (error)
- [ ] Extension indexer with `partial` modifier (error)
- [ ] Extension indexer with `protected` modifier (error)
- [ ] Extension indexer with `protected internal` modifier (error)
- [ ] Extension indexer with `private protected` modifier (error)
- [ ] Extension indexer with `init` accessor (error)

#### 1.5 Type Parameter Inferrability

- [ ] All type parameters used in extension parameter (valid)
- [ ] All type parameters used in indexer parameters (valid)
- [ ] All type parameters used across extension and indexer parameters (valid)
- [ ] Unused type parameter in extension block with indexer (error)
- [ ] Type parameter used only in return type (error)
- [ ] Type parameter used in accessor implementation but not signature (error)

#### 1.6 IndexerName Attribute

- [ ] Extension indexer without `[IndexerName]` attribute (default name: `Item`)
- [ ] Extension indexer with `[IndexerName("CustomName")]` attribute
- [ ] Multiple extension indexers in same extension with different `IndexerName` values
- [ ] Multiple extension indexers in same extension with same `IndexerName` (error)
- [ ] `IndexerName` reflects in metadata property name
- [ ] `IndexerName` reflects in `DefaultMemberAttribute`
- [ ] `IndexerName` reflects in implementation method names (`get_CustomName`, `set_CustomName`)

### 2. Consumption Tests

#### 2.1 Basic Indexer Access

- [ ] Access extension indexer with single argument
- [ ] Access extension indexer with multiple arguments
- [ ] Access extension indexer for reading (invokes get accessor)
- [ ] Access extension indexer for writing (invokes set accessor)
- [ ] Access extension indexer as ref return (reading)
- [ ] Access extension indexer as ref return (writing)
- [ ] Extension indexer on value type receiver
- [ ] Extension indexer on reference type receiver
- [ ] Extension indexer on nullable value type receiver
- [ ] Extension indexer on generic type receiver

#### 2.2 Lookup Priority (LDM 2026-03-09)

**Order: Real instance → Implicit instance → Real extension → Implicit extension**

- [ ] Real instance indexer takes priority over everything
- [ ] Inherited real instance indexer takes priority over everything
- [ ] Implicit instance indexer (Length/Count + this[int]) takes priority over extensions
- [ ] Inherited implicit instance indexer takes priority over extensions
- [ ] Real extension indexer used when no instance indexers (real or implicit) exist
- [ ] Real extension indexer used when instance indexers not applicable
- [ ] Implicit extension indexer used only when no real indexers (instance or extension) exist
- [ ] Extension indexer does NOT shadow compatible instance indexer
- [ ] Extension indexer used when instance indexer signature doesn't match

#### 2.3 Scope-Based Resolution

- [ ] Extension indexer in current type scope
- [ ] Extension indexer in enclosing type scope
- [ ] Extension indexer in namespace scope via `using`
- [ ] Extension indexer in `using static` scope
- [ ] Extension indexer in global `using`
- [ ] Extension indexer ambiguity within same scope (error)
- [ ] Extension indexer chosen from first applicable scope (no ambiguity with later scopes)
- [ ] Empty candidate set in first scope proceeds to next scope
- [ ] Extension indexer imported from another assembly

#### 2.4 Overload Resolution

- [ ] Single best extension indexer selected
- [ ] Ambiguous extension indexers in same scope (error)
- [ ] Extension indexer with better parameter match selected
- [ ] Extension indexer with better generic type inference selected
- [ ] Extension indexer overload with more specific parameter types
- [ ] Extension indexer with optional parameters vs required parameters
- [ ] Extension indexer with `params` vs explicit array parameter

#### 2.5 Generic Type Inference

- [ ] Type inference from receiver type
- [ ] Type inference from indexer argument types
- [ ] Type inference does NOT use value being assigned (set accessor) - **LDM 2026-02-02**
- [ ] Type inference with multiple type parameters
- [ ] Type inference with constraints on type parameters
- [ ] Type inference failure when types conflict (error)
- [ ] Explicit type arguments provided

#### 2.6 Special Cases

- [ ] Extension indexer not considered on `base` access (error)
- [ ] Extension indexer not considered when receiver is a type (error)
- [ ] Extension indexer not considered when argument has type `dynamic` (runtime binding)
- [ ] Extension indexer on `readonly struct` with ref return
- [ ] Extension indexer in `unsafe` context
- [ ] Extension indexer with null-conditional operator `?.[]`

### 3. Implicit Index/Range Indexer Tests

#### 3.1 Extension Count/Length Contributing (LDM 2026-02-02)

**Extensions contribute everywhere, including countable properties and implicit indexer fallback**

- [ ] Extension `Length` property makes type countable
- [ ] Extension `Count` property makes type countable
- [ ] Extension `Length` contributes to implicit `Index` indexer fallback
- [ ] Extension `Length` contributes to implicit `Range` indexer fallback
- [ ] Extension `Count` contributes to implicit `Index` indexer fallback
- [ ] Extension `Count` contributes to implicit `Range` indexer fallback

#### 3.2 Length vs Count Priority (LDM 2026-03-09)

**Within each scope, Length is prioritized over Count**

- [ ] Instance `Length` preferred over instance `Count`
- [ ] Instance `Length` preferred over extension `Count`
- [ ] Extension `Length` preferred over extension `Count` in same scope
- [ ] Instance `Count` preferred over extension `Length` (instance scope comes first)
- [ ] Extension `Length` in closer scope preferred over extension `Count` in outer scope

#### 3.3 Implicit Index Indexer

- [ ] Extension `Length` + extension `this[int]` enables `^index` syntax
- [ ] Extension `Count` + extension `this[int]` enables `^index` syntax
- [ ] Instance `Length` + extension `this[int]` enables `^index` syntax
- [ ] Extension `Length` + instance `this[int]` enables `^index` syntax
- [ ] Both parts of implicit indexer resolved independently (may come from different scopes)
- [ ] Implicit instance indexer tried after real instance indexer
- [ ] Implicit extension indexer tried after real extension indexer

#### 3.4 Implicit Range Indexer

- [ ] Extension `Length` + extension `Slice(int, int)` enables `range` syntax
- [ ] Extension `Count` + extension `Slice(int, int)` enables `range` syntax
- [ ] Instance `Length` + extension `Slice(int, int)` enables `range` syntax
- [ ] Extension `Length` + instance `Slice(int, int)` enables `range` syntax
- [ ] Both parts of implicit Range indexer resolved independently
- [ ] Implicit Range indexer tried after explicit `Range` indexer
- [ ] Classic extension method `Slice` contributes to implicit Range indexer (LDM 2026-03-09)
- [ ] New extension member `Slice` contributes to implicit Range indexer

#### 3.5 Lookup Ordering for Implicit Indexers (LDM 2026-03-09)

**Full lookup order:**
1. **Instance lookup only**: real indexer first
2. **Instance lookup only**: implicit indexer parts second
3. **Full lookup (instance+extension)**: real indexer third
4. **Full lookup (instance+extension)**: implicit indexer parts fourth (individual lookups per part)

- [ ] Real instance `Index` indexer found (step 1)
- [ ] Real instance `Range` indexer found (step 1)
- [ ] Implicit instance indexer parts found (step 2) when no real instance indexer
- [ ] Real extension `Index` indexer found (step 3) when no instance indexers
- [ ] Real extension `Range` indexer found (step 3) when no instance indexers
- [ ] Implicit extension indexer parts found (step 4) when no real indexers anywhere
- [ ] Each part of implicit extension indexer resolved in separate lookup
- [ ] Implicit extension Index indexer (`Length`/`Count` + `this[int]`)
- [ ] Implicit extension Range indexer (`Length`/`Count` + `Slice(int, int)`)

### 4. List Pattern Tests (LDM 2026-03-09)

**List patterns resolve Length/Count, Index indexer, and Range indexer individually**

#### 4.1 Extension Members in List Patterns

- [ ] Extension `Length` property makes type work in list patterns
- [ ] Extension `Count` property makes type work in list patterns
- [ ] Extension `this[Index]` enables element access in list patterns
- [ ] Extension `this[Range]` enables slice patterns with `..`
- [ ] List pattern with spread requires: Length/Count, Index indexer, Range indexer (all resolved independently)
- [ ] List pattern with simple elements `[var a, var b]`
- [ ] List pattern with slice `[.., var last]`
- [ ] List pattern with slice `[var first, .. var middle, var last]`

#### 4.2 List Pattern Lookup Order (LDM 2026-03-09)

**For Index and Range indexers in list patterns:**
- a. Instance lookup: real indexer first
- b. Instance lookup: implicit indexer parts second  
- c. Full lookup: real indexer third
- d. Full lookup: implicit indexer parts fourth (individual lookups per part)

- [ ] List pattern uses real instance `Index` indexer (step a)
- [ ] List pattern uses implicit instance `Index` indexer (step b)
- [ ] List pattern uses real extension `Index` indexer (step c)
- [ ] List pattern uses implicit extension `Index` indexer (step d)
- [ ] List pattern uses real instance `Range` indexer (step a)
- [ ] List pattern uses implicit instance `Range` indexer (step b)
- [ ] List pattern uses real extension `Range` indexer (step c)
- [ ] List pattern uses implicit extension `Range` indexer (step d)
- [ ] Length/Count, Index indexer, and Range indexer resolved independently (can come from different scopes)

#### 4.3 Length Pattern Behavior

- [ ] Non-negative handling kicks in when type is countable and indexable (via extensions)
- [ ] `Length` pattern `{ Length: > 5 }` works with extension `Length`
- [ ] Pattern matching on extension indexed elements

### 5. Other Element-Access Forms

#### 5.1 Null-Conditional Element Access

- [ ] Null-conditional indexer access `obj?[0]` with extension indexer
- [ ] Null-conditional with multiple indices `obj?[0, 1]`
- [ ] Null-conditional chaining `obj?[0]?[1]`

#### 5.2 Object and Collection Initializers

- [ ] Index initializer with extension indexer `new C { [0] = value }`
- [ ] Index initializer with multiple parameters `new C { [0, "key"] = value }`

#### 5.3 Collection Expressions (LDM 2026-03-09)

**Extension Length/Count does NOT contribute to spread optimization**

- [ ] Extension `Length`/`Count` does NOT enable spread optimization
- [ ] Collection expression spread with extension Length has no special optimization
- [ ] Collection expressions with types that have extension indexers (but no special behavior)

### 6. Expression Trees

- [ ] Extension indexer in expression tree compilation (error)
- [ ] Extension indexer accessor call in expression tree (error)
- [ ] Error message is clear and helpful

### 7. XML Documentation

#### 7.1 CREF Syntax

- [ ] CREF to extension indexer: `E.extension(int).this[string]`
- [ ] CREF to get accessor (with params): `E.extension(int).get_Item(string)`
- [ ] CREF to get accessor (no params): `E.extension(int).get_Item`
- [ ] CREF to set accessor (with params): `E.extension(int).set_Item(string, int)`
- [ ] CREF to set accessor (no params): `E.extension(int).set_Item`
- [ ] CREF to implementation method (get): `E.get_Item(int, string)`
- [ ] CREF to implementation method (get, no params): `E.get_Item`
- [ ] CREF to implementation method (set): `E.set_Item(int, string, int)`
- [ ] CREF to implementation method (set, no params): `E.set_Item`
- [ ] CREF with custom `IndexerName`: `E.extension(int).CustomName[string]`
- [ ] Documentation comments on extension indexer are preserved

### 8. Metadata and IL Emission

#### 8.1 Extension Property Emission

- [ ] Extension property named `Item` emitted by default
- [ ] Extension property named per `IndexerName` attribute when present
- [ ] Extension property accessors throw `NotImplementedException`
- [ ] Extension property has `[ExtensionMarkerName]` attribute
- [ ] Extension property points to correct marker type

#### 8.2 Implementation Methods

- [ ] `get_Item` static method emitted in enclosing class
- [ ] `set_Item` static method emitted in enclosing class
- [ ] Custom name (`get_CustomName`, `set_CustomName`) when `IndexerName` used
- [ ] Implementation methods prepend receiver parameter
- [ ] Implementation methods contain user-defined bodies
- [ ] Implementation methods are `static`
- [ ] Implementation methods participate in overload resolution
- [ ] Generic implementation methods preserve type parameters

#### 8.3 DefaultMemberAttribute

- [ ] `[DefaultMember("Item")]` emitted on grouping type by default
- [ ] `[DefaultMember("CustomName")]` emitted when `IndexerName` used
- [ ] Multiple indexers with same name result in single `DefaultMember`
- [ ] Multiple indexers with different names - verify behavior

#### 8.4 Attribute Preservation (LDM 2026-02-02)

**`[ParamArray]` attribute emitted on both get and set implementation methods**

- [ ] Attributes on extension indexer are emitted on extension property
- [ ] Attributes on get accessor are emitted on `get_Item` method
- [ ] Attributes on set accessor are emitted on `set_Item` method
- [ ] Attributes on parameters are emitted on implementation method parameters
- [ ] `[ParamArray]` attribute emitted on get implementation method with `params`
- [ ] `[ParamArray]` attribute emitted on set implementation method with `params` (LDM decision)
- [ ] Tooling handles `params` in non-final position correctly

### 9. Array and String Special Cases (LDM 2026-04-07)

**No extension indexers on arrays or strings**

#### 9.1 Arrays

- [ ] Extension indexer declared for array type (allowed in declaration)
- [ ] Extension indexer on array type NOT invoked at call site (error)
- [ ] Error message clearly states arrays don't support extension indexers
- [ ] Array element access always uses built-in array indexing
- [ ] Extension implicit indexers do not work on arrays
- [ ] Extension `Length` does not contribute to arrays
- [ ] Various array types: `int[]`, `string[]`, `T[]`, `int[,]`, `int[][]`

#### 9.2 Strings

- [ ] Extension indexer declared for string type (allowed in declaration)
- [ ] Extension indexer on string type NOT invoked at call site (error)
- [ ] Error message clearly states strings don't support extension indexers
- [ ] String element access always uses built-in string indexing
- [ ] Extension implicit indexers do not work on strings
- [ ] Extension `Length` does not contribute to strings

#### 9.3 Pointers

- [ ] Extension parameter may not be a pointer type (error in declaration)
- [ ] Clear error message for pointer extension parameter

### 10. Cross-Feature Interaction Tests

#### 10.1 With Other Extension Members

- [ ] Extension indexer and extension method in same extension block
- [ ] Extension indexer and extension property in same extension block
- [ ] Extension indexer and extension operator in same extension block
- [ ] Disambiguation via explicit static method call syntax

#### 10.2 With Nullable Reference Types

- [ ] Extension indexer with nullable receiver type
- [ ] Extension indexer with nullable parameter types
- [ ] Extension indexer with nullable return type
- [ ] Extension indexer get accessor returning nullable
- [ ] Extension indexer set accessor accepting nullable
- [ ] Nullability warnings when set value does NOT contribute to type inference (LDM 2026-02-02)

#### 10.3 With Async/Await

- [ ] Extension indexer accessor cannot be `async` (error)
- [ ] Extension indexer can return `Task<T>` (not awaited automatically)

#### 10.4 With Required Members

- [ ] Extension indexer used in object initializer with required members
- [ ] Index initializer after required member initialization

#### 10.5 With Collection Expressions

- [ ] Collection expression constructing type with extension indexer
- [ ] Collection expression spread does NOT use extension `Length`/`Count` for optimization (LDM 2026-03-09)

### 11. Error Scenarios

#### 11.1 Declaration Errors

- [ ] Extension indexer with `init` accessor (clear error message)
- [ ] Extension indexer with illegal modifier (clear error message per modifier)
- [ ] Extension indexer without receiver parameter name (clear error message)
- [ ] Extension indexer with unused type parameter (clear error message)
- [ ] Duplicate extension indexer signature in same extension (error)

#### 11.2 Consumption Errors

- [ ] No applicable extension indexer found (clear error message)
- [ ] Ambiguous extension indexer candidates (clear error with candidates listed)
- [ ] Extension indexer on `base` access (clear error message)
- [ ] Extension indexer with wrong argument types (clear error message)
- [ ] Extension indexer with wrong argument count (clear error message)
- [ ] Extension indexer in expression tree (clear error message)
- [ ] Extension indexer on array type (clear error message)
- [ ] Extension indexer on string type (clear error message)

#### 11.3 Metadata Errors

- [ ] Multiple extension indexers with conflicting `DefaultMemberAttribute` values
- [ ] Malformed metadata consumed by compiler

### 12. Interoperability Tests

#### 12.1 Cross-Language

- [ ] C# extension indexer consumed from C#
- [ ] C# extension indexer consumed from VB.NET (if supported)
- [ ] Extension indexer in .NET Standard 2.0 library
- [ ] Extension indexer in .NET Framework 4.7.2 library
- [ ] Extension indexer in .NET 8 library
- [ ] Extension indexer in .NET 10 library

#### 12.2 Reflection

- [ ] Extension indexer property visible via reflection
- [ ] Extension indexer implementation methods visible via reflection
- [ ] Extension indexer markers visible via reflection
- [ ] `DefaultMemberAttribute` readable via reflection
- [ ] Dynamic invocation of extension indexer implementation methods

#### 12.3 Source Generators

- [ ] Source generator creates extension indexer
- [ ] Source generator consumes extension indexer
- [ ] Source generator sees correct symbols for extension indexer

### 13. IDE Features

#### 13.1 IntelliSense

- [ ] Extension indexer appears in completion after `[`
- [ ] Extension indexer shows correct signature in parameter help
- [ ] Extension indexer shows documentation comments
- [ ] Extension indexer differentiated from instance indexers in UI
- [ ] Extension indexer shows scope information (namespace/using)
- [ ] Extension indexer priority hint in IntelliSense (shows after instance indexers)

#### 13.2 Navigation

- [ ] Go To Definition on extension indexer usage navigates to declaration
- [ ] Find All References finds all uses of extension indexer
- [ ] Go To Implementation navigates to implementation method
- [ ] Find All References on implementation method finds indexer uses

#### 13.3 Refactoring

- [ ] Rename extension indexer (via `IndexerName`)
- [ ] Rename extension indexer parameters
- [ ] Change signature of extension indexer
- [ ] Extract method containing extension indexer usage
- [ ] Inline extension indexer implementation method

#### 13.4 Code Fixes and Analyzers

- [ ] Code fix to add missing receiver parameter name
- [ ] Code fix to remove illegal modifier
- [ ] Code fix to resolve ambiguity (add explicit type arguments)
- [ ] Code fix to add `using` directive for extension indexer
- [ ] Warning/info about extension indexer not working on arrays
- [ ] Warning/info about extension indexer not working on strings

### 14. Performance Tests

- [ ] Extension indexer has no performance overhead vs manual implementation method call
- [ ] Generic type inference for extension indexer has acceptable performance
- [ ] Large number of extension indexers in scope doesn't cause slow compilation
- [ ] Overload resolution with many extension indexer candidates

### 15. Breaking Change Tests

- [ ] Existing code without extension indexers continues to work
- [ ] Existing extension methods named `get_Item`/`set_Item` don't conflict
- [ ] Adding extension indexer to existing type doesn't break existing code (proper lookup order)
- [ ] Existing implicit indexer usage continues to work when extension indexers are in scope
- [ ] Instance indexers always take priority (no breaking changes due to extensions)

### 16. Type Inference Edge Cases

#### 16.1 Set Value NOT Contributing to Inference (LDM 2026-02-02

From D:\GitHub\roslyn\src\Compilers\CSharp\Test\CSharp15\ExtensionIndexersTests.cs

## Test Categories

### 1. Language Version Support (`LangVer_01`)
- [x] Extension indexers require preview/next language version (C# 14 rejects, Next/Preview accepts)
- [x] Compilation reference vs assembly reference scenarios

### 2. Declaration Validation

#### Basic Declaration (`Declaration_01` - `Declaration_14`)
- [x] **Unnamed extension parameter** - Cannot declare instance members without named receiver
- [x] **Static indexers** - Not allowed in extension blocks
- [x] **Protected indexers** - Not allowed in extension blocks
- [x] **Non-inferrable type parameters** - Type parameter must be referenced
- [x] **Parameter naming conflicts**:
  - With extension parameter name
  - With extension type parameter
  - With enclosing static class (allowed)
- [x] **Field keyword** - Not supported in extension indexers
- [x] **Init-only accessors** - Not allowed in extension blocks
- [x] **Getter/setter named after enclosing class** - Produces appropriate error

#### Accessibility (`InconsistentTypeAccessibility_01`)
- [x] Inconsistent accessibility between parameter types and indexer
- [x] Inconsistent accessibility in return type

### 3. Indexing Operations

#### Basic Indexing (`Indexing_01` - `Indexing_59`)
- [x] Extension indexer when no instance indexer exists
- [x] Extension indexer when instance indexer not applicable
- [x] **Ambiguous extension indexers** - Multiple applicable candidates
- [x] **Instance indexer precedence** - Instance indexers take priority
- [x] **Obsolete indexers** - Proper warning generation
- [x] **Extension block scope resolution** - Multiple scopes tested
- [x] **Generic extension blocks**:
  - Simple generic receivers
  - Type parameters in indexer parameters
  - Constraint validation
- [x] **Named arguments** - Proper evaluation order
- [x] **Receiver conversion** - Implicit conversion scenarios
- [x] **Default parameters** - Support for optional parameters
- [x] **Params arrays** - Support in indexer parameters
- [x] **No getter scenarios** - Write-only indexers
- [x] **Inaccessible scenarios**:
  - File-scoped extensions
  - Private indexers
- [x] **Constant receivers** - Validation of assignment requirements
- [x] **Ref/in/out parameters**:
  - Ref receiver parameters
  - Ref readonly receiver parameters
  - In parameters in indexer
  - Ref/out in indexer parameters (not allowed)
- [x] **Ref-returning indexers**
- [x] **Private accessor** scenarios
- [x] **Type inference** - Generic type argument inference
- [x] **Missing type scenarios** - Handling missing dependencies
- [x] **Struct return values** - Cannot modify non-lvalue
- [x] **Indexing on type** - Error when indexing type name
- [x] **Extra ref modifiers** - Validation
- [x] **Null receivers** - Error handling
- [x] **Generic vs non-generic preference**
- [x] **Broken IL scenarios** - Malformed metadata handling
- [x] **Struct receiver conversions** - Boxing behavior

### 4. Bad Container Scenarios (`BadContainer_01` - `BadContainer_06`)
- [x] Lacking static enclosing type
- [x] Nested in extension block
- [x] In nested type
- [x] In generic type
- [x] In non-static enclosing type
- [x] __arglist parameter (invalid)

### 5. Implicit Index Indexers (`ImplicitIndexIndexer_01` - `ImplicitIndexIndexer_80`)
- [x] **Instance implicit indexer precedence** over extension `this[Index]`
- [x] **Extension Length + instance this[int]** combination
- [x] **Extension this[int] + instance Length** combination
- [x] **Extension Length + extension this[int]** in same scope
- [x] **Extension Length + extension this[int]** in different scopes
- [x] **Extension Count + extension this[int]** combination
- [x] **Instance Length + instance this[int]** takes precedence
- [x] **Extension this[Index] preferred** over implicit pattern
- [x] **Instance this[string] + extension this[Index]** selection
- [x] **Obsolete extension Length** property handling
- [x] **Obsolete extension this[int]** handling
- [x] **Receiver conversion scenarios** with implicit indexers
- [x] **Inheritance hierarchies**:
  - Length from base, this[int] from derived
  - Length from derived, this[int] from base
- [x] **Static extension Length** - Not applicable
- [x] **Extension Length without getter** - Not applicable
- [x] **Extension Length returning long** - Not applicable
- [x] **Ref-returning Length** - Not applicable
- [x] **Private getter on Length** - Not applicable
- [x] **Length method** (not property) - Not applicable
- [x] **Extension Count** - Alternative to Length
- [x] **Extension Length + this[string]** - Not applicable
- [x] **Extension Length + this[int, int]** - Not applicable
- [x] **Named argument scenarios** - Error handling
- [x] **Instance Count takes precedence** over instance Length
- [x] **Extension Length precedence** over extension Count
- [x] **Generic extension blocks** with implicit indexers
- [x] **Constrained generics** - Constraint satisfaction
- [x] **Ambiguous extensions** - Multiple applicable members
- [x] **Inaccessible type arguments** in generics
- [x] **Use-site diagnostics** - Missing dependent types
- [x] **Receiver side effects** evaluation
- [x] **Object initializers** with implicit indexers
- [x] **Instance Count + extension Length + extension this[int]**
- [x] **Instance Length + extension Count + extension this[int]**

### 6. Implicit Range Indexers (`ImplicitRangeIndexer_01` - `ImplicitRangeIndexer_50`)
- [x] **Instance implicit indexer (Slice) precedence** over extension `this[Range]`
- [x] **Extension Length + instance Slice** combination
- [x] **Extension Slice + instance Length** combination
- [x] **Extension Length + extension Slice** combination
- [x] **Extension Count + extension Slice** combination
- [x] **Cross-scope scenarios** - Inner/outer namespace resolution
- [x] **Extension this[Range] preference** over implicit pattern
- [x] **Instance Length + instance Slice** precedence
- [x] **Instance this[string] + extension this[Range]** selection
- [x] **Receiver conversion** with range indexers
- [x] **Inheritance scenarios** with Slice methods
- [x] **Static Slice methods** - Not applicable
- [x] **Property named Slice** - Not applicable
- [x] **Void-returning Slice** - Not applicable
- [x] **Wrong parameter counts** for Slice
- [x] **Wrong parameter types** for Slice
- [x] **Private Slice** - Not applicable
- [x] **Extension Length precedence** over extension Count
- [x] **Generic extension blocks** with range indexers
- [x] **Ambiguous Slice methods** - Multiple candidates
- [x] **Length from E1, Slice from E2** - Cross-type combinations
- [x] **Missing System.Int32** - Special handling
- [x] **Ambiguous inner Slice + outer Slice** scenarios
- [x] **Inapplicable instance Slice** + outer extension
- [x] **Classic extension Slice method** integration
- [x] **Inner/outer scope resolution** with Length and Slice
- [x] **Receiver side effects** with range indexers
- [x] **From-end range argument** evaluation order
- [x] **Instance Count + extension Length + extension Slice**
- [x] **Instance Length + extension Count + extension Slice**
- [x] **Static/dynamic parameter scenarios**
- [x] **Object/dynamic parameter types** - Not applicable

### 7. Special Implicit Indexers (`SpecialImplicitIndexIndexer_01` - `SpecialImplicitRangeIndexer_06`)
- [x] **String type** without Length/this[int] but with this[Index]
- [x] **Array type** scenarios with missing members
- [x] **Extension indexers on string** - Not applicable when string lacks proper members
- [x] **Extension indexers on Array** - Proper handling with ldlen
- [x] **String with this[Range]** indexer
- [x] **Array with this[Range]** indexer
- [x] **Slice patterns** on string/array with missing members
- [x] **Extension Length/Slice on string** - Special restrictions
- [x] **Extension Length/Slice on Array** - Array-specific behavior

### 8. Object Initializers (`ObjectInitializer_01` - `ObjectInitializer_ExtensionProperty_StructReceiver`)
- [x] Extension indexers in object initializers
- [x] Boxed receiver scenarios
- [x] Struct receiver with extension indexer
- [x] Struct receiver with extension property (comparison)

### 9. With Initializers (`WithInitializer_01`)
- [x] Extension indexers with with-expressions (struct)
- [x] Error scenarios with lvalue requirements

### 10. List Patterns (`ListPattern_01` - `ListPattern_25`)
- [x] **Extension Length + extension this[Index]** in list patterns
- [x] **Boxed receivers** in list patterns
- [x] **Nullable value type** receivers
- [x] **Optional parameters** in list pattern indexers
- [x] **Instance Length + extension this[int]** combination
- [x] **Ambiguous extension scenarios**:
  - Multiple Length properties
  - Multiple this[int] indexers
  - Multiple this[Index] indexers
- [x] **Extension Length + instance this[Index]**
- [x] **Generic extension** Length + this[Index]
- [x] **Separate generic extensions** for Length and indexer
- [x] **Extension Length only** - Incomplete pattern
- [x] **Multi-scope resolution**:
  - Inner Length + outer this[int]
  - Inner Length + outer this[Index]
  - Ambiguous inner + outer resolutions
- [x] **Classic extension Slice** integration

### 11. Slice Patterns (`SlicePattern_01` - `SlicePattern_02`)
- [x] Extension Length + this[Index] + this[Range] in slice patterns
- [x] Boxed receiver in slice patterns
- [x] Language version requirements for slice patterns

---

## Test Infrastructure
- Uses `CombilingTestBase` for compilation tests
- Supports both compilation and metadata references (`useCompilationReference` parameter)
- Tests against multiple target frameworks (Net70, Net100, Net Framework 4.7.2)
- Includes IL verification where applicable
- Tests semantic model API (`GetSymbolInfo`, `GetMemberGroup`, `GetTypeInfo`)

## Coverage Summary by Feature Area

| Feature Area | Test Count (approx) | Lines Covered |
|--------------|---------------------|---------------|
| Language Version | 1 | 87-127 |
| Declaration | 14 | 129-422 |
| Basic Indexing | 59 | 424-2073 |
| Bad Containers | 6 | 2075-2237 |
| Implicit Index | 80 | 2239-5918 |
| Implicit Range | 50 | 5920-7982 |
| Special Implicit | 6 | 7984-9481 |
| Object Initializers | 3 | 9483-9644 |
| With Initializers | 1 | 9646-9690 |
| List Patterns | 25 | 9692-10025 |
| Slice Patterns | 2+ | (partial coverage through 10025) |

## Key Testing Patterns
1. **Compilation verification** - Successful compilation validation
2. **Diagnostic verification** - Expected error/warning messages
3. **Execution verification** - Runtime behavior with `CompileAndVerify`
4. **IL verification** - Generated IL correctness
5. **Symbol validation** - Semantic model correctness
6. **Cross-scope testing** - Namespace and using directive scenarios
7. **Type system integration** - Generics, inheritance, conversions
8. **Edge cases** - Null, missing types, malformed metadata

---





## 1. Slice Pattern Tests (SlicePattern_01 through SlicePattern_21)

### 1.1 Language Version Tests
- **SlicePattern_03**: Extension Length + extension indexer + extension Slice with language version checking
  - Verifies C# 13 errors for extension indexers
  - Verifies preview/next feature availability

### 1.2 Scope and Precedence Tests
- **SlicePattern_04**: Instance Length + inner extension indexer/Slice + outer extension Index/Range indexers
- **SlicePattern_05**: Inner extension Length/indexer/Slice + outer extension Index/Range indexers
- **SlicePattern_06**: Ambiguous extension Length handling (should fail)
- **SlicePattern_07**: Extension Length + Count (Length takes precedence)
- **SlicePattern_08**: Ambiguous extension indexer handling
- **SlicePattern_09**: Ambiguous extension Slice handling
- **SlicePattern_10**: Inapplicable extension Length
- **SlicePattern_11**: Inapplicable extension indexer (uses outer Range indexer)
- **SlicePattern_12**: Inapplicable extension Slice
- **SlicePattern_13**: Inapplicable extension Range indexer (uses outer Range indexer)

### 1.3 Generic Extension Tests
- **SlicePattern_14**: Generic extension Length + Index indexer + Slice
- **SlicePattern_15**: Generic extension Length + Index/Range indexers
- **SlicePattern_16**: Extension Length + Index indexer + generic Slice (should fail)

### 1.4 Array Type Tests
- **SlicePattern_17**: Array with extension Index/Range indexers (native array behavior preserved)
- **SlicePattern_17_WithExtensionLength**: Array with/without Length using extension Length
  - Tests both with normal array and array without Length member

### 1.5 Mixed Member Tests
- **SlicePattern_18**: Instance Length + inner extension Length/indexer + outer extension Slice
- **SlicePattern_18_2**: Instance Length + extension indexer + extension Slice
- **SlicePattern_18_3**: Instance Length + extension Index/Range indexers
- **SlicePattern_19**: Instance Length + instance indexer + inner extension Length/Slice
- **SlicePattern_20**: Extension Length + extension indexer + classic extension Slice
- **SlicePattern_21**: Struct receiver passed by value with extension Length + Index/Range indexers

---

## 2. Conditional Operations Tests

### 2.1 Conditional Assignment
- **ConditionalAssignment_01**: Null-conditional assignment with extension indexer setter
  - Language version testing (C# 14, next, preview)
  - Null vs non-null receiver behavior
- **ConditionalAssignment_02**: Nullable value type receiver (tracked by issue #79451)

### 2.2 Conditional Access
- **ConditionalAccess_01**: Null-conditional access with extension indexer getter
  - Language version testing
  - Null vs non-null receiver behavior
- **ConditionalAccess_02**: Nullable value type receiver with IL verification

---

## 3. Miscellaneous Operation Tests

### 3.1 Nameof Expression
- **Nameof_01**: Extension indexer in nameof (should error - no name)
- **Nameof_02**: Setter-only extension indexer in nameof

### 3.2 Overload Resolution Priority Attribute (ORPA)
- **ORPA_01**: ORPA on extension indexer (lower priority indexer)
- **ORPA_02**: ORPA on extension indexer (higher priority indexer)
- **ORPA_03**: ORPA across different extension classes

---

## 4. Metadata and Emit Tests (Metadata_01 through Metadata_04)

### 4.1 Basic Metadata
- **Metadata_01**: Single extension indexer with type IL verification
  - Verifies nested type structure
  - Verifies ExtensionAttribute and DefaultMemberAttribute
  - Verifies skeleton/implementation method patterns

### 4.2 Reference Usage
- **Metadata_02**: Extension indexer consumption via compilation and PE references
  - Language version testing (C# 14)
  - Verifies symbol representations

### 4.3 IndexerName Attribute
- **Metadata_03**: Single indexer with IndexerName attribute
  - Verifies custom indexer name in metadata
  - Verifies accessor method names match
- **Metadata_04**: Multiple indexers with matching IndexerName attributes

---

## 5. IndexerName Tests (IndexerName_01 through IndexerName_03)

- **IndexerName_01**: Mismatched IndexerName attributes (should error)
- **IndexerName_02**: IndexerName same as extension parameter (allowed)
- **IndexerName_03**: IndexerName same as enclosing static class (allowed)

---

## 6. Extern Tests (Extern_03, Extern_05, Extern_09 through Extern_11)

### 6.1 P/Invoke
- **Extern_03**: Extension indexer with DllImport attribute
  - IL verification for pinvokeimpl methods
  - Skeleton vs implementation method patterns

### 6.2 Extern Without DllImport
- **Extern_05**: Extern extension indexer without DllImport (warnings expected)
  - Comparison with regular class indexer behavior

### 6.3 MethodImplOptions
- **Extern_09**: Extern extension indexer with MethodImplOptions.InternalCall

### 6.4 Error Cases
- **Extern_10**: DllImport on non-extern extension indexer (should error)
- **Extern_11**: Extern extension indexer with body (should error)

---

## 7. Symbol Lookup Tests (LookupSymbols_01 through LookupSymbols_03)

### 7.1 Non-Generic Extensions
- **LookupSymbols_01**: Symbol lookup by various names
  - Lookup by "this[]" (reduced extension methods only)
  - Lookup by "Item" (not found)
  - Lookup by "get_Item" (found)

### 7.2 Generic Extensions
- **LookupSymbols_02**: Symbol lookup with generic extension block
  - Type substitution verification
  - Comparison with instance indexers

### 7.3 Under-Inferred Generics
- **LookupSymbols_03**: Symbol lookup when type parameter not fully inferred

---

## 8. Nullability Tests - Indexing (Nullability_Indexing_01 through Nullability_Indexing_37)

### 8.1 Basic Nullability
- **Nullability_Indexing_01**: Maybe-null receiver with generic indexer
- **Nullability_Indexing_02**: String indexer (built-in) with null warning
- **Nullability_Indexing_03**: Maybe-null argument with generic indexer
- **Nullability_Indexing_04**: Named arguments with nullability
- **Nullability_Indexing_05**: Warning in receiver expression
- **Nullability_Indexing_06**: Warning in argument expression
- **Nullability_Indexing_07**: Chained indexer access

### 8.2 Parameter Nullability
- **Nullability_Indexing_09**: Indexer parameter disallows null
- **Nullability_Indexing_10**: Ref extension parameter with object (should error)
- **Nullability_Indexing_11**: Generic indexer return value nullability
- **Nullability_Indexing_12**: Ref extension parameter with nullability warnings
- **Nullability_Indexing_13**: In extension parameter with nullability warnings

### 8.3 Nullability Attributes
- **Nullability_Indexing_14**: NotNullIfNotNull attribute (issue #37238 - not yet supported on indexers)
- **Nullability_Indexing_15**: NotNull attribute on indexer
- **Nullability_Indexing_16**: MaybeNull attribute on indexer
- **Nullability_Indexing_17**: AllowNull attribute on indexer
- **Nullability_Indexing_18**: DisallowNull attribute on indexer
- **Nullability_Indexing_19**: DoesNotReturn attribute (issue #50018 - not yet supported on indexers)
- **Nullability_Indexing_20**: NotNullWhen attribute on extension parameter
- **Nullability_Indexing_21**: MaybeNullWhen attribute on extension parameter
- **Nullability_Indexing_22**: MemberNotNull attribute (issue #78828 - extension member post-conditions)

### 8.4 Type Parameter Nullability
- **Nullability_Indexing_23**: Type parameter as receiver
- **Nullability_Indexing_24**: Un-annotated extension parameter nullability check
- **Nullability_Indexing_25**: Annotated extension parameter nullability check
- **Nullability_Indexing_26**: Return value nullability checking
- **Nullability_Indexing_27**: Set value nullability checking
- **Nullability_Indexing_28**: Compound assignment nullability
- **Nullability_Indexing_29**: Generic extension parameter property read access
- **Nullability_Indexing_30**: Generic extension parameter property write access
- **Nullability_Indexing_31**: Notnull constraint on generic extension
- **Nullability_Indexing_32**: Notnull constraint in tuple

### 8.5 Reference Conversions
- **Nullability_Indexing_34**: Implicit reference conversion on receiver (nullable extension parameter)
- **Nullability_Indexing_35**: Implicit reference conversion on receiver (non-nullable extension parameter)

### 8.6 Optional Parameters
- **Nullability_Indexing_36**: Optional parameter in extension indexer
- **Nullability_Indexing_37**: Optional parameter with nullability warning

---

## 9. Nullability Tests - Object Initializers (Nullability_ObjectInitializer_01 through Nullability_ObjectInitializer_21)

### 9.1 Basic Scenarios
- **Nullability_ObjectInitializer_01**: Generic indexer with null argument
- **Nullability_ObjectInitializer_02**: Named and reordered arguments
- **Nullability_ObjectInitializer_03**: Nested initializer nullability
- **Nullability_ObjectInitializer_04**: Nullable vs non-nullable assignments
- **Nullability_ObjectInitializer_05**: Target-typed new with generic indexer
- **Nullability_ObjectInitializer_06**: Target-typed new with generic indexer parameter
- **Nullability_ObjectInitializer_07**: Generic type parameter in indexer

### 9.2 Type Inference
- **Nullability_ObjectInitializer_08**: Complex type inference scenarios
  - Flow state tracking with conditional checks
  - Record type constructors
- **Nullability_ObjectInitializer_09**: Notnull constraint scenarios
- **Nullability_ObjectInitializer_10**: Consumer pattern with type inference

### 9.3 Nested Initializers
- **Nullability_ObjectInitializer_11**: Nested object initializer (generic)
- **Nullability_ObjectInitializer_12**: Nested object initializer (target-typed)
- **Nullability_ObjectInitializer_13**: Nested initializer with type parameter
- **Nullability_ObjectInitializer_14**: Nested target-typed with type parameter
- **Nullability_ObjectInitializer_15**: Nested initializer without getter (error)
- **Nullability_ObjectInitializer_16**: Nested initializer without setter (error)
- **Nullability_ObjectInitializer_17**: Nested initializer with setter

### 9.4 Error Cases
- **Nullability_ObjectInitializer_18**: No applicable extension indexer
- **Nullability_ObjectInitializer_19**: Optional parameters in initializer
- **Nullability_ObjectInitializer_20**: Target-typed with argument warnings
- **Nullability_ObjectInitializer_21**: Ref extension parameter in initializer (error)

---

## 10. Nullability Tests - Increment Operations (Nullability_Increment_01 through Nullability_Increment_05)

- **Nullability_Increment_01**: Nullable type with increment operator
- **Nullability_Increment_02**: DisallowNull attribute with increment
- **Nullability_Increment_03**: Non-nullable type with increment
- **Nullability_Increment_04**: MaybeNull attribute with increment (warning)
- **Nullability_Increment_05**: Null suppression with increment and MaybeNull

---

## 11. Nullability Tests - List Patterns (Nullability_ListPattern_01)

- **Nullability_ListPattern_01**: List pattern with generic vs non-generic types
  - Extension indexer used for list pattern element access
  - Nullability of captured variables

---

## 12. XML Documentation Tests (Cref_01)

- **Cref_01**: XML cref attribute resolution for extension indexers
  - References to `this[type]`
  - References to `get_Item`/`set_Item`
  - Invalid references (this[], Item)
  - Language version compatibility (C# 13 errors)

---


# Extension Indexers Test Plan

## XML Documentation Comments (Cref)

### Cref_01 - Cref_04
- **Cref_01**: Language version enforcement for cref references to extension indexers
- **Cref_02**: LangVer of consuming compilation affects cref resolution  
- **Cref_03**: Cref resolution with `[IndexerName]` attribute on extension indexer
- **Cref_04**: Cref resolution for generic extension indexers with type parameters

## XML Documentation Generation (XmlDoc)

### XmlDoc_01 - XmlDoc_02
- **XmlDoc_01**: XML documentation generation for extension indexers with type parameters and parameters, including `<typeparamref>` and `<paramref>` references
- **XmlDoc_02**: XML documentation on individual accessor (get/set) are not processed (warning CS1587)

## Caller Information Attributes

### CallerArgumentExpression_01 - CallerArgumentExpression_20
- **CallerArgumentExpression_01**: `[CallerArgumentExpression]` attribute referring to extension parameter in indexer parameters
- **CallerArgumentExpression_02**: Self-referential `[CallerArgumentExpression]` warning
- **CallerArgumentExpression_06**: Caller expression refers to last parameter of indexer
- **CallerArgumentExpression_16**: Multiple caller argument expression parameters
- **CallerArgumentExpression_17**: `[CallerArgumentExpression]` referring to `value` parameter (error)
- **CallerArgumentExpression_18**: In indexer access expression
- **CallerArgumentExpression_19**: In object initializer
- **CallerArgumentExpression_20**: In list pattern

### CallerMemberName_01 - CallerMemberName_04
- **CallerMemberName_01**: `[CallerMemberName]` in extension indexer parameter
- **CallerMemberName_02**: CallerMemberName called from local function within indexer getter reports indexer name
- **CallerMemberName_03**: CallerMemberName with `[IndexerName]` attribute
- **CallerMemberName_04**: CallerMemberName with `ref readonly` parameter

## Element Access Behavior

### ElementAccess_01 - ElementAccess_03
- **ElementAccess_01**: Pointer type receiver disallowed (CS1103)
- **ElementAccess_02**: Array indexing prioritizes built-in array indexer over extension indexer
- **ElementAccess_03**: String indexing prioritizes built-in string indexer over extension indexer

## Using Directives

### Usings_01 - Usings_04
- **Usings_01**: Extension indexer found via `using` namespace directive
- **Usings_02**: Extension indexer found via `using static` directive
- **Usings_03**: Multiple namespaces with extension indexers, best match selected
- **Usings_04**: Extension indexer used when other namespace has unrelated extension members

## COM Interop

### RefOmittedComCall_01 - RefOmittedComCall_02
- **RefOmittedComCall_01**: Extension indexers with ref/out parameters disallowed for COM import types
- **RefOmittedComCall_02**: Ref extension parameter on COM import type properly rejected

## Dynamic

### Dynamic_01 - Dynamic_05
- **Dynamic_01**: Dynamic argument to extension indexer reports appropriate error (CS0021)
- **Dynamic_02**: Dynamic argument with multiple overloads
- **Dynamic_04**: Dynamic parameter type on extension indexer (works)
- **Dynamic_05**: Runtime error when using dynamic receiver with extension indexer

## Miscellaneous Validation

### CheckAndCoerceArguments_01
- Irregular/legacy behavior - pointer types allowed in indexer argument position (no safety check)

### ConditionalAttribute_01 - ConditionalAttribute_02
- **ConditionalAttribute_01**: `[Conditional]` attribute not valid on indexer
- **ConditionalAttribute_02**: `[Conditional]` attribute not valid on indexer accessor

## Ref Safety Analysis

### RefAnalysis_Indexing_01 - RefAnalysis_Indexing_11
- **RefAnalysis_Indexing_01**: Returning ref from local variable through extension indexer (errors)
- **RefAnalysis_Indexing_02**: Returning ref local initialized from local through extension indexer (errors)
- **RefAnalysis_Indexing_03**: Ref-safety with ref struct receiver and return types
- **RefAnalysis_Indexing_07**: Ref readonly ref-assignment narrower escape scope
- **RefAnalysis_Indexing_08**: Ref-like escape mixing with Span parameter (readonly get modifier error)
- **RefAnalysis_Indexing_09**: Span parameter ref-safety with non-ref-struct receiver
- **RefAnalysis_Indexing_10**: Span parameter ref-safety with ref-struct receiver (by-value)
- **RefAnalysis_Indexing_11**: Span parameter ref-safety with ref-struct receiver (by-ref) - escape errors

### RefAnalysis_ObjectCreation_01 - RefAnalysis_ObjectCreation_08
- **RefAnalysis_ObjectCreation_01**: Returning ref from local in object creation context
- **RefAnalysis_ObjectCreation_02**: Span parameter in object initializer with ref struct receiver (by-value)
- **RefAnalysis_ObjectCreation_03**: Span parameter in object initializer with ref struct receiver (by-ref) - escape error
- **RefAnalysis_ObjectCreation_04**: Ref-returning indexer in object initializer
- **RefAnalysis_ObjectCreation_05**: Evaluation order in object initializer with extension indexer
- **RefAnalysis_ObjectCreation_06**: Span parameter with ref returning indexer in object initializer
- **RefAnalysis_ObjectCreation_07**: Span parameter with class receiver in object initializer
- **RefAnalysis_ObjectCreation_08**: Evaluation order with struct receiver in object initializer

## Synthesized Parameter Attributes

### SynthesizedAttributeOnParameters_In_01
- `[IsReadOnly]` attribute synthesized on `in` extension parameter, verified through PE metadata

## IOperation Tree Representation

### IOperation_01 - IOperation_10
- **IOperation_01**: Simple assignment to extension indexer
- **IOperation_02**: List pattern with extension Index indexer
- **IOperation_03**: List pattern with extension Length and instance this[Index]
- **IOperation_04**: Slice pattern with extension Range indexer
- **IOperation_05**: Object initializer with extension indexer
- **IOperation_06**: Null-conditional indexing with extension indexer (setter)
- **IOperation_07**: Null-conditional indexing with extension indexer (getter) with null-coalescing
- **IOperation_08**: Compound assignment with ref-returning indexer and extension operator
- **IOperation_09**: Implicit indexer (Index/^) with extension Length and instance this[int]
- **IOperation_10**: Implicit indexer with extension Length but no this[int] (error)

## Missing Compiler Members

### MissingMembers_01
- Missing `DefaultMemberAttribute` produces CS0656 error

## Nullability Analysis

### Nullability_ReceiverConversion_01 - Nullability_ReceiverConversion_10
- **Nullability_ReceiverConversion_01**: Nullability mismatch when converting receiver for extension indexer
- **Nullability_ReceiverConversion_02**: Nullability in object initializer
- **Nullability_ReceiverConversion_03**: Target-typed `new()` in initializer - error CS0021
- **Nullability_ReceiverConversion_04**: Target-typed `new()` with matching nullability
- **Nullability_ReceiverConversion_05**: Nested object initializer with nullability mismatch
- **Nullability_ReceiverConversion_06**: List pattern - no applicable indexer error
- **Nullability_ReceiverConversion_07**: List pattern with matching extension Length/indexer
- **Nullability_ReceiverConversion_08**: `with` expression - indexers not supported in with (CS0131, CS0747)
- **Nullability_ReceiverConversion_09**: Increment operator nullability
- **Nullability_ReceiverConversion_10**: Compound assignment nullability on property

### Nullability_Setter_01 - Nullability_Setter_26
- **Nullability_Setter_01**: Generic extension parameter with notnull constraint - write access warnings
- **Nullability_Setter_02**: Generic extension parameter without constraint - write access warnings  
- **Nullability_Setter_03**: Warnings in receiver expression during write access
- **Nullability_Setter_04**: Assignment result nullability (generic extension parameter)
- **Nullability_Setter_05**: Variance in receiver (covariant interface)
- **Nullability_Setter_06**: Variance in receiver (contravariant interface)
- **Nullability_Setter_08**: Ref-returning property nullability
- **Nullability_Setter_09**: Indexer state tracking doesn't affect subsequent access
- **Nullability_Setter_10**: Assignment with maybe-null implicit conversion
- **Nullability_Setter_11**: Assignment with not-null implicit conversion
- **Nullability_Setter_13**: Compound assignment nullability (generic)
- **Nullability_Setter_14**: Compound assignment with `[DisallowNull]` attribute
- **Nullability_Setter_15**: Compound assignment with nullable-returning operator
- **Nullability_Setter_16**: Compound assignment with non-nullable-returning operator
- **Nullability_Setter_17**: Compound assignment with nullable-returning operator, ref-returning property
- **Nullability_Setter_18**: Compound assignment with non-nullable-returning operator, ref-returning property
- **Nullability_Setter_19**: Compound assignment result value nullability with nullable-returning operator
- **Nullability_Setter_20**: Compound assignment result value nullability with non-nullable-returning operator
- **Nullability_Setter_21**: Compound assignment warning in receiver
- **Nullability_Setter_22**: Compound assignment warning in RHS
- **Nullability_Setter_23**: Compound assignment warning in indexer argument
- **Nullability_Setter_24**: Compound assignment result with maybe-null conversion
- **Nullability_Setter_25**: Compound assignment result with not-null conversion
- **Nullability_Setter_26**: Compound assignment ref-returning with null-suppression operator

### Nullability_Params_01 - Nullability_Params_10
- **Nullability_Params_01**: Params array of non-nullable reference type - null warnings in indexing
- **Nullability_Params_02**: Params array of nullable reference type - no warnings
- **Nullability_Params_03**: Params array with generic type parameter
- **Nullability_Params_04**: Params and return both generic - nullability enforced
- **Nullability_Params_05**: Params and return generic with null element - dereference warning
- **Nullability_Params_06**: Params in object initializer - null warnings
- **Nullability_Params_07**: Params in object initializer - null allowed (nullable array)
- **Nullability_Params_08**: Params in compound assignment - null warnings
- **Nullability_Params_09**: Params in compound assignment - null allowed
- **Nullability_Params_10**: Params in disambiguation invocation - null warnings, set_Item signature validation

## Dynamic Indexer Access Edge Cases

### DynamicIndexerAccess_11 - DynamicIndexerAccess_12
- **DynamicIndexerAccess_11**: Nullability checking in dynamic member initializer
- **DynamicIndexerAccess_12**: Nullability checking in target-typed dynamic member initializer

## Interpolated String Handler Integration

### InterpolationHandler_ReceiverParameter_WithOtherParameters
- Interpolated string handler with receiver parameter and additional parameters, handler arguments reference both

### InterpolationHandler_ReceiverParameter_WithConversion
- Receiver conversion from derived type to base type for handler argument

### InterpolationHandler_ReceiverParameter_WithConversion_ExtensionParameterNarrowerThanConstructor
- Extension parameter narrower than handler constructor parameter (derived to base conversion)

### InterpolationHandler_ReceiverParameter_WithConversion_ExtensionParameterWiderThanConstructor
- Extension parameter wider than handler constructor parameter - error CS1503

### InterpolationHandler_ReceiverParameter_ByRef
- `ref` extension parameter passed to handler constructor with mutation tracking

### InterpolationHandler_ReceiverParameter_ByRef_WithConstantReceiver  
- `ref` extension parameter with constant receiver - error CS1510 (not assignable)

### InterpolationHandler_ReceiverParameter_Generic_ByRef
- Generic ref extension parameter with type constraint

### InterpolationHandler_ReceiverParameter_ByIn_WithConstantReceiver
- `in`/`ref readonly` extension parameter with constant receiver (copies to temp)

### InterpolationHandler_ReceiverParameter_ByIn_WithLocalReceiver
- `in` extension parameter with local variable receiver

### InterpolationHandler_ReceiverParameter_ByRefMismatch_01
- Extension parameter `in`/`ref readonly`/value but handler expects `ref` - error CS1620

### InterpolationHandler_ReceiverParameter_ByRefMismatch_02  
- Extension parameter `ref` but handler expects `in`/`ref readonly` (allowed)

### InterpolationHandler_ReceiverParameter_ByRefMismatch_03
- Extension parameter `ref` but handler expects by-value - error CS1615

### InterpolationHandler_StructReceiverParameter_ByValue
- Struct receiver by-value, mutations don't persist

### InterpolationHandler_StructReceiverParameter_ByValueThroughField
- Struct receiver accessed through field, handler receives copy






1. Interpolated String Handler Integration Tests
1.1 InterpolationHandler with Struct Receiver (Generic, By Value Through Field)
•	Test: InterpolationHandler_StructReceiverParameter_Generic_ByValueThroughField
•	Scenarios:
•	Extension indexer with interpolated string handler that references receiver parameter
•	Generic struct receiver passed by value through field
•	Validation of correct evaluation order and side effects
•	Tests both indexer syntax (field[i, $""]) and explicit method call (E.get_Item(field, i, $""))
•	Tests generic method without struct constraint and with struct constraint
•	Verifies IL generation for both constrained and unconstrained generic scenarios
•	Tests compilation reference vs metadata reference
1.2 InterpolationHandler with Struct Receiver - Compound Assignment
•	Test: InterpolationHandler_StructReceiverParameter_Generic_ByValueThroughField_CompoundAssignment
•	Scenarios:
•	Extension indexer with compound assignment (field[i, $""] += 0)
•	Interpolated string handler argument on setter
•	Generic struct receiver
•	Verifies proper evaluation order with side effects
1.3 InterpolationHandler with GenericStruct Receiver
•	Test: InterpolationHandler_StructReceiverParameter_GenericStruct_ByValueThroughField
•	Scenarios:
•	Extension with struct constraint (where T : struct)
•	Interpolated string handler parameter
•	Tests with struct constraint only (no unconstrained version)
1.4 InterpolationHandler with Class Receiver (GenericClass)
•	Test: InterpolationHandler_ClassReceiverParameter_GenericClass_ByValueThroughField
•	Scenarios:
•	Extension indexer on generic class receiver
•	By value through field with class type
•	Tests both unconstrained generic and class-constrained generic
•	Verifies proper reference capture behavior
1.5 InterpolationHandler with Class Receiver (Generic)
•	Test: InterpolationHandler_ClassReceiverParameter_Generic_ByValueThroughField
•	Scenarios:
•	Extension with class constraint (where T : class)
•	Interpolated string handler referencing receiver
1.6 InterpolationHandler with RefStruct Receiver - Escape Scopes
•	Test: InterpolationHandler_RefStructReceiverParameter_EscapeScopes_01 through _07
•	Scenarios:
•	Ref struct receiver with escape scope validation
•	Tests with scoped modifier on receiver parameter
•	Tests with scoped modifier on handler parameter
•	Validates that ref struct references don't escape their declaration scope
•	Verifies IL generation for safe scenarios
•	Tests various combinations of scoped parameters
1.7 InterpolationHandler with Receiver Parameter - Nullable Mismatch
•	Tests: InterpolationHandler_ReceiverParameter_NullableMismatch_01, _02
•	Scenarios:
•	Nullable reference type warnings for interpolation handler constructor
•	Tests receiver parameter nullability vs handler constructor parameter nullability
•	Tests with optional out bool valid parameter
•	Verifies warnings for null arguments passed to non-nullable parameters
1.8 InterpolationHandler Parameter Errors
•	Tests: InterpolationHandler_ParameterErrors_MappedCorrectly_01, _02, _03
•	Scenarios:
•	Invalid InterpolatedStringHandlerArgument attribute with non-existent parameter name
•	InterpolatedStringHandlerArgument on extension parameter itself (disallowed)
•	InterpolatedStringHandlerArgument with empty string (references receiver, which is invalid)
•	Validates that error tracking correctly maps to symbols
1.9 InterpolationHandler References Instance Parameter from Metadata
•	Test: InterpolationHandler_ReferencesInstanceParameter_FromMetadata
•	Scenarios:
•	IL-based test where interpolation handler attribute is malformed in metadata
•	Validates error reporting for malformed attributes from metadata
1.10 InterpolationHandler as Extension Parameter
•	Test: InterpolationHandler_AsExtensionParameter
•	Scenarios:
•	Interpolated string handler type used as extension receiver parameter
•	Verifies this is rejected (interpolated strings construct handler, not receivers)
1.11 InterpolationHandler in Object Initializer
•	Tests: InterpolationHandler_ObjectInitializer_01, _02
•	Scenarios:
•	Extension indexer with interpolation handler in object initializer syntax
•	Tests both simple assignment ([42, $"{43}"] = 1) and nested initializer ([42, $"{43}"] = { Field = 0 })
•	Verifies operation tree and control flow graph
•	Validates proper evaluation order in initializers
2. Use-Site Error Tests
2.1 List Pattern with Missing Type Extensions
•	Tests: UseSite_01 through UseSite_07
•	Scenarios:
•	Extension Length property on missing type
•	Extension this[Index] on missing type
•	Extension this[Range] on missing type
•	Extension this[int] on missing type
•	Extension Slice(int, int) on missing type
•	Extension indexer returning missing type
•	Extension Slice returning missing type
•	Validates proper use-site error reporting when referenced assemblies are missing
3. Length Pattern Tests
3.1 Negative Length Pattern Tests
•	Tests: LengthPattern_NegativeLengthTest, _02, _03
•	Scenarios:
•	Extension Length property used in patterns with negative values
•	Tests { Length: -1 } pattern (should be unreachable)
•	Tests { Length: -1 or 1 } pattern (first arm redundant)
•	Tests switch expressions with impossible patterns
•	Tests on object type and nullable types
•	Tests with separate Length in different namespaces
4. Collection and Pattern Tests
4.1 Collection Spread
•	Test: CollectionSpread_01
•	Scenarios:
•	Extension Length property not used in collection expressions
•	Collection spread syntax ([0, .. c]) with IEnumerable type
4.2 Loop with Pattern Declaration
•	Tests: LoopWithPatternDeclaration_ListPattern, _SlicePattern, _SlicePattern_SliceMethod
•	Scenarios:
•	Generic extension indexer/Length in list pattern within loop
•	Nullable flow analysis with list patterns
•	Slice pattern with Range indexer
•	Slice pattern with Slice method
•	Validates nullability warnings
5. Extension Slice Tests
5.1 Extension Slice Methods
•	Tests: ExtensionSlice_01 through _04
•	Scenarios:
•	Classic extension Slice(this Span<int>, int, int) method
•	Extension block Slice(int, int) method
•	Slice with open-ended range and extension methods
•	Tests on custom Span type with missing instance Slice
6. Array Tests
6.1 Array Extension Indexer
•	Test: Array_01
•	Scenarios:
•	Extension indexer on array type
•	Validates that built-in array indexer takes precedence
7. Implicit Index (System.Index) Indexer Access Tests
7.1 LValue Receiver Tests (Struct)
7.1.1 Struct By Value
•	Tests: ImplicitIndexIndexerAccess_Get_LValueReceiver_01, _01_02, _01_03
•	Scenarios:
•	Extension this[int] + extension Length (both extension)
•	Extension this[int] + instance Length (hybrid)
•	Instance this[int] + extension Length (hybrid)
•	Struct lvalue receiver passed by value
•	Tests with implicit Index conversion (this[^1])
•	Validates evaluation order and side effects
•	Verifies IL generation
7.1.2 Struct By Ref
•	Tests: ImplicitIndexIndexerAccess_Get_LValueReceiver_02, _02_02, _02_03
•	Scenarios:
•	Extension receiver with ref, ref readonly, or in modifiers
•	Extension this[int] + extension Length
•	Extension this[int] + instance Length
•	Instance this[int] + extension Length
•	Tests with rvalue expressions (should error for ref, warn for ref readonly, ok for in)
•	Validates ref-return behavior
7.2 LValue Receiver Tests (Class)
•	Tests: ImplicitIndexIndexerAccess_Get_LValueReceiver_03, _03_02, _03_03
•	Scenarios:
•	Class lvalue receiver passed by value
•	Extension this[int] + extension Length
•	Extension this[int] + instance Length
•	Instance this[int] + extension Length
•	Validates reference semantics and side effects
7.3 LValue Receiver Tests (Generic Struct)
•	Tests: ImplicitIndexIndexerAccess_Get_LValueReceiver_04, _04_02, _04_02_02, _04_02_03, _04_03
•	Scenarios:
•	Generic struct lvalue receiver
•	Tests unconstrained generic (<T>)
•	Tests struct-constrained generic (<T> where T : struct)
•	Tests with interface constraint (<T> where T : IHasLength/IHasIndexer)
•	Validates constrained call codegen
•	Tests async scenarios with await
7.4 LValue Receiver Tests (Generic Struct with Ref)
•	Tests: ImplicitIndexIndexerAccess_Get_LValueReceiver_05, _05_02, _05_03
•	Scenarios:
•	Generic struct with ref extension parameter (extension<T>(ref T x) where T : struct)
•	Tests with interface constraints
•	Tests async scenarios
•	Validates that in and ref readonly require concrete value types for generics
7.5 LValue Receiver Tests (Generic Class)
•	Tests: ImplicitIndexIndexerAccess_Get_LValueReceiver_06, _06_02, _06_03
•	Scenarios:
•	Generic class lvalue receiver
•	Tests unconstrained generic and class-constrained generic
•	Tests with interface constraints
•	Validates by-value semantics with reference types
7.6 RValue Receiver Tests (Struct)
•	Tests: ImplicitIndexIndexerAccess_Get_RValueReceiver_01, _02
•	Scenarios:
•	Struct rvalue receiver (method return value)
•	Extension receiver passed by value
•	Extension receiver passed by ref/ref readonly/in
•	Validates proper temporary handling
•	Tests error reporting for invalid ref parameters on rvalues
7.7 RValue Receiver Tests (Class)
•	Test: ImplicitIndexIndexerAccess_Get_RValueReceiver_03
•	Scenarios:
•	Class rvalue receiver passed by value
•	Method return value as receiver
7.8 RValue Receiver Tests (Generic Struct)
•	Tests: ImplicitIndexIndexerAccess_Get_RValueReceiver_04, _04_02, _04_03
•	Scenarios:
•	Generic struct rvalue receiver
•	Tests unconstrained, struct-constrained, and interface-constrained generics
•	Tests async scenarios with await
•	Validates proper boxing behavior
7.9 RValue Receiver with Object Creation
•	Test: ImplicitIndexIndexerAccess_Get_LValueReceiver_04_04
•	Scenarios:
•	Extension indexer in object initializer with Index
•	new T() { [^1] = { Property = 42 } }
•	Tests with generic type parameter
•	Validates member initializer syntax
7.10 RValue Receiver Tests (Ref Generic Struct)
•	Test: ImplicitIndexIndexerAccess_Get_RValueReceiver_05
•	Scenarios:
•	Generic struct with ref extension parameter
•	Rvalue receiver (should error - ref requires lvalue)
•	Tests async scenarios


1. Implicit Index Indexer Access - Get Operations
1.1 L-Value Receivers (Lines 25052-26423)
Struct L-Value Receivers:
•	Test: ImplicitIndexIndexerAccess_Get_LValueReceiver_01 & variants (_02, _03)
•	Extension Length + Extension this[int] (passed by value)
•	Instance Length + Extension this[int] (passed by value)
•	Extension Length + Instance this[int] (passed by value)
•	Verification: Static field access and instance method access with evaluation order
•	Test: ImplicitIndexIndexerAccess_Get_LValueReceiver_02 (Theory: ref, ref readonly, in)
•	Extension Length + Extension this[int] (passed by ref)
•	Verification: Different ref kinds with proper IL generation
•	Test: ImplicitIndexIndexerAccess_Get_LValueReceiver_02_02 & _02_03
•	Instance Length + Extension this[int] (passed by ref)
•	Extension Length + Instance this[int] (passed by ref)
Class L-Value Receivers:
•	Test: ImplicitIndexIndexerAccess_Get_LValueReceiver_03 & variants (_02, _03)
•	Extension Length + Extension this[int] (class, by value)
•	Instance Length + Extension this[int]
•	Extension Length + Instance this[int]
•	Verification: Reference type receiver state mutation
Generic Struct L-Value Receivers:
•	Test: ImplicitIndexIndexerAccess_Get_LValueReceiver_04 & variants (_02, _03, _04)
•	Unconstrained T and where T : struct
•	Extension Length + Extension this[int]
•	Instance Length (via interface) + Extension this[int]
•	Extension Length + Instance this[int] (via interface)
•	Object creation expressions with implicit index
•	Verification: Complex generic IL patterns, boxing/unboxing
•	Test: ImplicitIndexIndexerAccess_Get_LValueReceiver_05 & variants (_02, _03)
•	where T : struct with ref parameter
•	Instance Length (via interface) + Extension this[int] (by ref)
•	Extension Length (by ref) + Instance this[int] (via interface)
•	Async await scenarios
•	Error cases: Rvalue receiver with ref parameter
Generic Class L-Value Receivers:
•	Test: ImplicitIndexIndexerAccess_Get_LValueReceiver_06 & variants (_02, _03)
•	Unconstrained T and where T : class
•	Extension Length + Extension this[int]
•	Instance Length (via interface) + Extension this[int]
•	Extension Length + Instance this[int] (via interface)
1.2 R-Value Receivers (Lines 25052-26423)
Struct R-Value Receivers:
•	Test: ImplicitIndexIndexerAccess_Get_RValueReceiver_01 & variants (_02, _03)
•	Extension Length + Extension this[int] (passed by value)
•	Instance Length + Extension this[int]
•	Extension Length + Instance this[int]
•	Test: ImplicitIndexIndexerAccess_Get_RValueReceiver_02 (Theory: ref, ref readonly, in)
•	Error/Warning cases: Ref parameter with rvalue receiver
•	Proper diagnostics for ERR_RefLvalueExpected and WRN_RefReadonlyNotVariable
Class R-Value Receivers:
•	Test: ImplicitIndexIndexerAccess_Get_RValueReceiver_03 & variants (_02, _03)
•	Extension Length + Extension this[int]
•	Instance Length + Extension this[int]
•	Extension Length + Instance this[int]
Generic R-Value Receivers:
•	Test: ImplicitIndexIndexerAccess_Get_RValueReceiver_04 & variants (_02, _03)
•	Struct rvalue: Extension Length + Extension this[int]
•	Instance Length (via interface) + Extension this[int]
•	Extension Length + Instance this[int] (via interface)
•	Async scenarios
•	Test: ImplicitIndexIndexerAccess_Get_RValueReceiver_05 & variants (_02, _03)
•	where T : struct with ref receiver
•	Error cases: Rvalue with ref parameter (ERR_RefLvalueExpected)
•	Test: ImplicitIndexIndexerAccess_Get_RValueReceiver_06 & variants (_02, _03)
•	Generic class rvalue receivers
•	All combinations of extension/instance Length and indexer
 
2. Implicit Range Indexer Access - Get Operations (Lines 25280-28416)
2.1 L-Value Receivers
Struct L-Value Receivers:
•	Test: ImplicitRangeIndexerAccess_Get_LValueReceiver_01 & variants (_02, _03)
•	Extension Length + Extension Slice(int, int) (passed by value)
•	Instance Length + Extension Slice
•	Extension Length + Instance Slice
•	Verification: IL verification for Range lowering
•	Test: ImplicitRangeIndexerAccess_Get_LValueReceiver_02 (Theory: ref, ref readonly, in)
•	Extension Length + Extension Slice (passed by ref)
•	Variants with instance Length or instance Slice
Class L-Value Receivers:
•	Test: ImplicitRangeIndexerAccess_Get_LValueReceiver_03 & variants (_02, _03)
•	Extension Length + Extension Slice
•	Instance/extension combinations
•	Reference type state mutations
Generic Struct L-Value Receivers:
•	Test: ImplicitRangeIndexerAccess_Get_LValueReceiver_04 & variants (_02, _03, _04)
•	Unconstrained T and where T : struct
•	Interface-constrained scenarios
•	Object creation with Range expressions
•	Test: ImplicitRangeIndexerAccess_Get_LValueReceiver_05 & variants (_02, _03)
•	where T : struct with ref receiver
•	Async scenarios with Range
•	Error cases: Rvalue with ref parameter
Generic Class L-Value Receivers:
•	Test: ImplicitRangeIndexerAccess_Get_LValueReceiver_06 & variants (_02, _03)
•	Unconstrained T and where T : class
•	Interface-based Length or Slice members
2.2 R-Value Receivers
Struct R-Value Receivers:
•	Test: ImplicitRangeIndexerAccess_Get_RValueReceiver_01 & variants (_02, _03)
•	Extension Length + Extension Slice (passed by value)
•	Instance/extension combinations
•	Test: ImplicitRangeIndexerAccess_Get_RValueReceiver_02 (Theory: ref, ref readonly, in)
•	Error/Warning cases: Ref parameter with rvalue
•	Variants with instance/extension combinations
Class R-Value Receivers:
•	Test: ImplicitRangeIndexerAccess_Get_RValueReceiver_03 & variants (_02, _03)
•	All extension/instance combinations
Generic R-Value Receivers:
•	Test: ImplicitRangeIndexerAccess_Get_RValueReceiver_04 & variants (_02, _03)
•	Unconstrained T and where T : struct
•	Interface constraints
•	Async scenarios
•	Test: ImplicitRangeIndexerAccess_Get_RValueReceiver_05 & variants (_02, _03)
•	where T : struct with ref receiver
•	Error cases: ERR_RefLvalueExpected
•	Test: ImplicitRangeIndexerAccess_Get_RValueReceiver_06 & variants (_02, _03)
•	Generic class rvalue receivers with all combinations
 
3. Reference Analysis (Lines 28418-28537)
•	Test: ImplicitIndexIndexerAccess_RefAnalysis_01
•	Ref struct with ref field escape analysis
•	Extension implicit this[Index] escape checking
•	Instance this[int] + Instance Length (baseline)
•	Instance this[Index] (baseline)
•	Verification: ERR_EscapeVariable and ERR_EscapeCall diagnostics
•	Test: ImplicitRangeIndexerAccess_RefAnalysis_01
•	Ref struct with Slice escape analysis
•	Extension Length + Extension Slice
•	Verification: ERR_EscapeVariable and ERR_EscapeCall diagnostics
 
4. Range Expression Variants (Lines 28540-30101)
4.1 start.. (Int Start, Open End)
Struct L-Value Receivers:
•	Test: ImplicitRangeIndexerAccess_Get_LValueReceiver_IntStartOpenEndRangeExpr_01 & variants (_02, _03)
•	Extension Length + Extension Slice (by value)
•	Instance Length + Extension Slice
•	Extension Length + Instance Slice
•	Verification: Evaluation order, state mutation
•	Test: ImplicitRangeIndexerAccess_Get_LValueReceiver_IntStartOpenEndRangeExpr_02 (Theory: ref, ref readonly, in)
•	By-ref variants of above
Class L-Value Receivers:
•	Test: ImplicitRangeIndexerAccess_Get_LValueReceiver_IntStartOpenEndRangeExpr_03 & variants (_02, _03)
•	All extension/instance combinations
Generic L-Value Receivers:
•	Test: ImplicitRangeIndexerAccess_Get_LValueReceiver_IntStartOpenEndRangeExpr_04
•	Unconstrained T and where T : struct
•	Test: ImplicitRangeIndexerAccess_Get_LValueReceiver_IntStartOpenEndRangeExpr_05
•	where T : struct with ref receiver
•	Async scenarios
•	Test: ImplicitRangeIndexerAccess_Get_LValueReceiver_IntStartOpenEndRangeExpr_06
•	Unconstrained T and where T : class
4.2 ..endIndex (Open Start, Index End)
•	Test: ImplicitRangeIndexerAccess_Get_LValueReceiver_OpenStartIndexEndRangeExpr_01
•	Struct lvalue with extension Length + extension Slice
•	Verification: ..endIndex lowering with Index.GetOffset
4.3 start..endIndex (Int Start, Index End)
•	Test: ImplicitRangeIndexerAccess_Get_LValueReceiver_IntStartIndexEndRangeExpr_01
•	Struct lvalue with all combinations
•	Verification: Combined int and Index handling
•	Test: ImplicitRangeIndexerAccess_Get_LValueReceiver_IntStartIndexEndRangeExpr_03
•	Class lvalue with all combinations
4.4 ^end.. (Hat Start, Open End)
•	Test: ImplicitRangeIndexerAccess_Get_LValueReceiver_HatStartOpenEndRangeExpr_01
•	Struct lvalue with extension Length + extension Slice
•	Verification: From-end index (^) handling in start position
4.5 start..end (Int Start, Int End)
•	Test: ImplicitRangeIndexerAccess_Get_LValueReceiver_IntStartIntEndRangeExpr_01
•	Struct lvalue with extension Length + extension Slice
•	Verification: Simple int-to-int Range lowering (no GetOffset calls)


# Extension Indexers Test Plan - Lines 30101-35679

## Overview
This test plan covers scenarios for extension indexers with implicit Index and Range support in C# 15. The tests validate code generation, receiver evaluation order, and various receiver types (struct/class, lvalue/rvalue, generic/non-generic).

---

## 1. Implicit Range Indexer Access - Get Operations (Open-End Range: `start..`)

### 1.1 Struct Receivers with `start..` Syntax
- **Test**: `ImplicitRangeIndexerAccess_Get_RValueReceiver_IntStartOpenEndRangeExpr_01`
  - Struct rvalue receiver, passed by value to extension Length + extension Slice
  - Range expression: `start..` with int start
  - Validates receiver evaluation before index, proper state management

- **Test**: `ImplicitRangeIndexerAccess_Get_RValueReceiver_IntStartOpenEndRangeExpr_02`
  - Struct rvalue receiver, passed by ref/ref readonly/in to extension Length + extension Slice
  - Range expression: `start..` with int start
  - Theory with `ref`, `ref readonly`, `in` ref kinds
  - **Expected**: `ref` produces CS1510 (ref lvalue expected), `ref readonly` produces CS9193 warning, `in` compiles cleanly

### 1.2 Class Receivers with `start..` Syntax
- **Test**: `ImplicitRangeIndexerAccess_Get_RValueReceiver_IntStartOpenEndRangeExpr_03`
  - Class rvalue receiver passed by value (reference type)
  - Extension Length + extension Slice
  - Range expression: `start..`

### 1.3 Generic Receivers with `start..` Syntax
- **Test**: `ImplicitRangeIndexerAccess_Get_RValueReceiver_IntStartOpenEndRangeExpr_04`
  - Generic struct rvalue receiver passed by value
  - Unconstrained T (could be struct or class)
  - T constrained to struct
  - Async test with `await` in start expression
  - Validates boxing behavior for unconstrained generic

- **Test**: `ImplicitRangeIndexerAccess_Get_RValueReceiver_IntStartOpenEndRangeExpr_05`
  - Generic struct rvalue receiver passed by ref
  - Ref-extension on rvalue receiver
  - **Expected**: CS1510 error (ref lvalue expected)

- **Test**: `ImplicitRangeIndexerAccess_Get_RValueReceiver_IntStartOpenEndRangeExpr_06`
  - Generic class rvalue receiver passed by value
  - Unconstrained T (could be struct or class)
  - T constrained to class
  - Async test with `await` in start expression

---

## 2. Receiver Swap Scenarios (Unconstrained Generic + Reference Types)

### 2.1 Method Invocation Receiver Swap
- **Test**: `RefTypeReceiverSwap_01`
  - Method invocation on unconstrained-T receiver
  - Argument mutates receiver via ref parameter
  - Validates that receiver is captured before argument evaluation

### 2.2 Explicit Indexer Assignment Receiver Swap
- **Test**: `RefTypeReceiverSwap_02`
  - Explicit indexer regular assignment instead of method invocation
  - Argument mutates receiver via ref parameter

### 2.3 Implicit Index Indexer Receiver Swap
- **Test**: `RefTypeReceiverSwap_03`
  - Implicit Index indexer (`^1`) instead of explicit indexer
  - Argument mutates receiver via ref parameter

### 2.4 Implicit Range Indexer Read Receiver Swap
- **Test**: `RefTypeReceiverSwap_04`
  - Implicit Range indexer read (`start..`)
  - Argument mutates receiver via ref parameter

### 2.5 Implicit Range Indexer Write Receiver Swap
- **Test**: `RefTypeReceiverSwap_05`
  - Implicit Range indexer with ref-returning Slice setter
  - Argument mutates receiver via ref parameter

---

## 3. Implicit Index Indexer Access - Set Operations

### 3.1 Struct Receivers
- **Test**: `ImplicitIndexIndexerAccess_Set_01`
  - Struct receiver, extension implicit `this[Index]`
  - Extension Length + extension `this[int]` setter
  - Test1: static field receiver
  - Test2: instance method receiver (`this`)
  - **Note**: Receiver not a variable produces CS0131

- **Test**: `ImplicitIndexIndexerAccess_Set_01_02`
  - Struct receiver, instance `this[int]` + extension Length
  - Validates interaction between instance members and extension members

- **Test**: `ImplicitIndexIndexerAccess_Set_01_03`
  - Struct receiver, extension `this[int]` + instance Length
  - Validates interaction between extension members and instance members

### 3.2 Struct Receivers Passed by Ref
- **Test**: `ImplicitIndexIndexerAccess_Set_02`
  - Struct receiver passed by ref/ref readonly/in
  - Extension implicit `this[Index]`
  - Theory with `ref`, `ref readonly`, `in` ref kinds
  - **For non-variable receivers**:
    - `ref`: CS1510 (two diagnostics for Length and indexer)
    - `ref readonly`: CS9193 warning (three times) + CS0131
    - `in`: CS0131 only

### 3.3 Class Receivers
- **Test**: `ImplicitIndexIndexerAccess_Set_03`
  - Class receiver, extension implicit `this[Index]`
  - Extension Length + extension `this[int]` setter
  - Validates receiver evaluation order with state mutations

### 3.4 Generic Receivers - Unconstrained
- **Test**: `ImplicitIndexIndexerAccess_Set_04`
  - Generic receiver passed by value, extension implicit `this[Index]`
  - Test1: unconstrained T (could be struct or class)
  - Test2: T constrained to struct
  - **Note**: Commented out Test3 due to issue #79416 (async)
  - **For non-variable receivers with struct constraint**: no lvalue requirement for by-value extension

### 3.5 Generic Receivers - Object Creation Scenarios
- **Test**: `ImplicitIndexIndexerAccess_Set_LValueReceiver_04_04`
  - Generic struct lvalue receiver, extension implicit `this[Index]`, object creation with instance Property
  - Test1: unconstrained T with `new()`
  - Test2: T constrained to struct
  - Test3: async with `await`

- **Test**: `ImplicitIndexIndexerAccess_Set_LValueReceiver_04_05`
  - Generic struct lvalue receiver, extension implicit `this[Index]`, object creation with extension Property
  - Test1: unconstrained T with `new()`
  - Test3: async with `await`

- **Test**: `ImplicitIndexIndexerAccess_Set_LValueReceiver_04_06`
  - Generic struct lvalue receiver, extension implicit `this[Index]`, object creation direct assignment
  - Test1: unconstrained T with `new()`
  - Test2: T constrained to struct
  - Test3: async with `await`

### 3.6 Generic Receivers - Struct Constrained with Ref
- **Test**: `ImplicitIndexIndexerAccess_Set_05`
  - Ref struct-constrained extension parameter, extension implicit `this[Index]`
  - Test2: struct constraint
  - Test3: async causes CS8178 (ref-returning call across await boundary)
  - **For non-variable receivers**: CS1510 (two diagnostics)

### 3.7 Generic Receivers - Class Constrained
- **Test**: `ImplicitIndexIndexerAccess_Set_06`
  - Generic extension parameter, extension implicit `this[Index]`, class-constrained receiver
  - Test1: unconstrained T
  - Test2: T constrained to class
  - **Note**: Commented out Test3 due to issue #79416 (async)

### 3.8 Generic Rvalue Receivers
- **Test**: `ImplicitIndexIndexerAccess_Set_07`
  - Generic rvalue struct receiver, extension implicit `this[Index]`
  - Test1: unconstrained generic
  - Test3: async causes CS8178 (ref-returning call across await boundary)

- **Test**: `ImplicitIndexIndexerAccess_Set_08`
  - Generic rvalue class receiver, extension implicit `this[Index]`
  - Test1: unconstrained generic
  - Test2: class-constrained generic
  - Test3: async causes CS8178 (ref-returning call across await boundary)

---

## 4. Implicit Range Indexer Access - Set Operations

### 4.1 Struct Receivers
- **Test**: `ImplicitRangeIndexerAccess_Set_01`
  - Struct receiver, extension Length + ref-returning extension Slice
  - Test1: static field receiver
  - Test2: instance method receiver (`this`)
  - **For non-variable receivers**: compiles successfully (can assign to ref-return immediately)

- **Test**: `ImplicitRangeIndexerAccess_Set_01_02`
  - Struct receiver, instance Length + ref-returning extension Slice
  - Validates proper IL generation

- **Test**: `ImplicitRangeIndexerAccess_Set_01_03`
  - Struct receiver, extension Length + ref-returning instance Slice
  - Validates proper IL generation

### 4.2 Struct Receivers Passed by Ref
- **Test**: `ImplicitRangeIndexerAccess_Set_02`
  - Struct receiver passed by ref/ref readonly/in
  - Extension Length + ref-returning extension Slice
  - Theory with `ref`, `ref readonly`, `in` ref kinds
  - **For non-variable receivers**:
    - `ref`: CS1510 (two diagnostics)
    - `ref readonly`: compiles with CS9193 warnings (three times)
    - `in`: compiles cleanly

- **Test**: `ImplicitRangeIndexerAccess_Set_02_02`
  - Struct receiver passed by ref/ref readonly/in
  - Instance Length + ref-returning extension Slice
  - Theory with `ref`, `ref readonly`, `in` ref kinds
  - **For non-variable receivers**:
    - `ref`: CS1510
    - `ref readonly`: compiles with CS9193 warning
    - `in`: compiles cleanly

- **Test**: `ImplicitRangeIndexerAccess_Set_02_03`
  - Struct receiver passed by ref/ref readonly/in
  - Extension Length + ref-returning instance Slice
  - Theory with `ref`, `ref readonly`, `in` ref kinds

### 4.3 Class Receivers
- **Test**: `ImplicitRangeIndexerAccess_Set_03`
  - Class receiver, extension Length + ref-returning extension Slice
  - Validates receiver evaluation order with state mutations

- **Test**: `ImplicitRangeIndexerAccess_Set_03_02`
  - Class receiver, instance Length + ref-returning extension Slice

- **Test**: `ImplicitRangeIndexerAccess_Set_03_03`
  - Class receiver, extension Length + ref-returning instance Slice

### 4.4 Generic Receivers - Value Type
- **Test**: `ImplicitRangeIndexerAccess_Set_04`
  - Generic extension parameter, extension Length + ref-returning extension Slice, struct-constrained receiver
  - Test1: unconstrained T
  - Test2: struct-constrained T
  - **For non-variable receivers with struct constraint**: compiles successfully

### 4.5 Generic Receivers - Object Creation Scenarios
- **Test**: `ImplicitRangeIndexerAccess_Set_LValueReceiver_04_04`
  - Generic struct lvalue receiver, extension implicit `this[Range]`, object creation
  - Test1: unconstrained T with `new()`
  - Test2: struct-constrained T
  - Test3: async with `await`

### 4.6 Generic Receivers - Struct Constrained with Ref
- **Test**: `ImplicitRangeIndexerAccess_Set_05`
  - Ref struct-constrained extension parameter, extension Length + ref-returning extension Slice
  - Test2: struct constraint
  - Test3: async causes CS8178 (ref-returning call across await boundary)
  - **For non-variable receivers**: CS1510 (two diagnostics)

### 4.7 Generic Receivers - Class Constrained
- **Test**: `ImplicitRangeIndexerAccess_Set_06`
  - Generic extension parameter, extension Length + ref-returning extension Slice, class-constrained receiver
  - Test1: unconstrained T
  - Test2: class-constrained T

### 4.8 Generic Rvalue Receivers
- **Test**: `ImplicitRangeIndexerAccess_Set_07`
  - Generic rvalue struct receiver, extension Length + ref-returning extension Slice
  - Test1: generic struct
  - Test3: async causes CS8178 (ref-returning call across await boundary)

- **Test**: `ImplicitRangeIndexerAccess_Set_08`
  - Generic rvalue class receiver, extension Length + ref-returning extension Slice
  - Test1: unconstrained generic
  - Test2: class-constrained generic
  - Test3: async causes CS8178 (ref-returning call across await boundary)

---

## 5. Range Expression Variants - Set Operations

### 5.1 Int Start, Open End (`start..`)
- **Test**: `ImplicitRangeIndexerAccess_Set_IntStartOpenEndRangeExpr_01`
  - Struct receiver, extension Length + ref-returning extension Slice
  - Range: `GetStart()..`
  - Test1: static field receiver
  - Test2: instance method receiver (`this`)

### 5.2 Hat Start, Open End (`^start..`)
- **Test**: `ImplicitRangeIndexerAccess_Set_HatStartOpenEndRangeExpr_01`
  - Struct receiver, extension Length + ref-returning extension Slice
  - Range: `^GetStart()..`
  - Test1: static field receiver
  - Test2: instance method receiver (`this`)

### 5.3 Int Start, Int End (`start..end`)
- **Test**: `ImplicitRangeIndexerAccess_Set_IntStartIntEndRangeExpr_01`
  - Struct receiver, extension Length + ref-returning extension Slice
  - Range: `GetStart()..GetEnd()`
  - Test1: static field receiver
  - Test2: instance method receiver (`this`)

### 5.4 Int Start, Index End (`start..^end`)
- **Test**: `ImplicitRangeIndexerAccess_Set_IntStartIndexEndRangeExpr_01`
  - Struct receiver, extension Length + ref-returning extension Slice
  - Range: `GetStart()..^GetEnd()`
  - Test1: static field receiver
  - Test2: instance method receiver (`this`)

### 5.5 Open Start, Index End (`..^end`)
- **Test**: `ImplicitRangeIndexerAccess_Set_OpenStartIndexEndRangeExpr_01`
  - Struct receiver, extension Length + ref-returning extension Slice
  - Range: `..^GetEnd()`
  - Test1: static field receiver
  - Test2: instance method receiver (`this`)

---

## 6. Object Creation Scenarios

### 6.1 Non-Generic Struct
- **Test**: `ImplicitIndexIndexerAccess_Set_LValueReceiver_04_06_01`
  - Non-generic struct receiver
  - Extension implicit `this[Index]`, object creation
  - Test1: synchronous
  - Test2: async with `await`

### 6.2 Non-Generic Class
- **Test**: `ImplicitIndexIndexerAccess_Set_LValueReceiver_04_06_02`
  - Non-generic class receiver
  - Extension implicit `this[Index]`, object creation
  - Test1: synchronous
  - Test2: async with `await`

---

## 7. Rvalue Receiver Scenarios

### 7.1 Struct Rvalue Receivers - Error Cases
- **Test**: `ImplicitIndexIndexerAccess_Set_RValueReceiver_01`
  - Struct rvalue receiver, extension implicit `this[Index]` setter
  - **Expected**: CS1612 (cannot modify return value, not a variable)
  - **Tracked by**: Issue #79451 (consider adjusting receiver requirements for extension members)

### 7.2 Class Rvalue Receivers - Valid Cases
- **Test**: `ImplicitIndexIndexerAccess_Set_RValueReceiver_03`
  - Class rvalue receiver, extension implicit `this[Index]` setter
  - Validates proper evaluation order of receiver, index, and value expressions

---

## 8. Generic Receivers with Interface Constraints

### 8.1 Struct Constraint with Interface
- **Test**: `ImplicitIndexIndexerAccess_Set_LValueReceiver_04_02_02`
  - Generic struct lvalue receiver, instance Length via `IHasLength` interface + extension `this[int]`
  - Struct constraint + interface constraint
  - Validates constrained callvirt for interface property

### 8.2 Class Constraint with Interface
- **Test**: `ImplicitIndexIndexerAccess_Set_LValueReceiver_04_02_03`
  - Generic class lvalue receiver, instance Length via `IHasLength` interface + extension `this[int]`
  - Class constraint + interface constraint
  - Object creation scenario

---

## 9. Ref Safety Analysis

### 9.1 Index Indexer Ref Safety
- **Test**: `ImplicitIndexIndexerAccess_Set_RefAnalysis_01`
  - Struct rvalue indexer setter with ref struct
  - **Expected**: CS0131 (left-hand side must be variable, property, or indexer)
  - Tests both extension indexer and instance indexer scenarios

### 9.2 Range Indexer Ref Safety
- **Test**: `ImplicitRangeIndexerAccess_Set_RefAnalysis_01`
  - Ref-returning Slice used immediately for assignment compiles successfully
  - Returning ref from Slice across escape scope produces CS8347 + CS8352

---

## Test Validation Criteria

### Code Generation
- Proper IL generation for receiver handling
- Correct evaluation order: receiver → indices → Length/Count → Slice computation → value (for setters)
- Appropriate boxing for unconstrained generics vs. direct calls for constrained generics

### Diagnostics
- **CS1510**: A ref or out value must be an assignable variable
- **CS9193**: Argument should be a variable because it is passed to a 'ref readonly' parameter
- **CS0131**: The left-hand side of an assignment must be a variable, property or indexer
- **CS1612**: Cannot modify the return value because it is not a variable
- **CS8178**: A reference returned by a call cannot be preserved across 'await' or 'yield' boundary
- **CS8347/CS8352**: Ref safety errors for escaping references

# Extension Indexers Test Plan Summary
## ExtensionIndexersTests.cs (Lines 35679-42124)

This document summarizes the test scenarios covered for extension indexers with implicit Index/Range support in C# 15.

---

## 1. Implicit Index Indexer Access - Compound Assignment (`+=`)

### 1.1 Struct Extension Parameter Tests

#### Test: `ImplicitIndexIndexerAccess_CompoundAssignment_01`
- **Scenario**: Extension indexer and Length on struct, compound assignment with `^1` index
- **Variations**: Static method & instance method
- **Expected**: Proper evaluation order and side effects for struct value types
- **IL Verification**: Confirms receiver, index, length, getter, and setter evaluation order
- **Edge Case**: Verifies receiver must be a variable (error CS0131 for `default(S1)[^1]`)

#### Test: `ImplicitIndexIndexerAccess_CompoundAssignment_01_02`
- **Scenario**: Instance indexer + extension Length on struct
- **Expected**: Same evaluation order, different IL pattern
- **Edge Case**: Non-variable receiver rejected

#### Test: `ImplicitIndexIndexerAccess_CompoundAssignment_01_03`
- **Scenario**: Extension indexer + instance Length on struct
- **Expected**: Proper mixing of extension and instance members
- **Edge Case**: Non-variable receiver rejected

### 1.2 Ref/In/Ref Readonly Extension Parameter Tests

#### Test: `ImplicitIndexIndexerAccess_CompoundAssignment_02` (Theory: ref, ref readonly, in)
- **Scenario**: Struct passed by reference to extension
- **Variations**: `ref`, `ref readonly`, `in` parameter modifiers
- **Expected**: Proper handling of by-ref struct values
- **IL Verification**: Different code paths for each ref kind
- **Edge Cases**:
  - `ref` → Error CS1510 for non-variable receiver (twice)
  - `ref readonly` → Warning CS9193 + Error CS0131 for non-variable receiver
  - `in` → Error CS0131 for non-variable receiver

#### Test: `ImplicitIndexIndexerAccess_CompoundAssignment_02_02` (Theory: ref, ref readonly, in)
- **Scenario**: Instance indexer + extension Length, ref struct
- **Expected**: Similar behavior with different member combinations

#### Test: `ImplicitIndexIndexerAccess_CompoundAssignment_02_03` (Theory: ref, ref readonly, in)
- **Scenario**: Extension indexer + instance Length, ref struct
- **Expected**: Proper member resolution and evaluation

### 1.3 Class Extension Parameter Tests

#### Test: `ImplicitIndexIndexerAccess_CompoundAssignment_03`
- **Scenario**: Class receiver with mutable state changes during evaluation
- **Expected**: Receiver captured once, subsequent mutations don't affect access
- **IL Verification**: Single load of receiver before evaluation

#### Test: `ImplicitIndexIndexerAccess_CompoundAssignment_03_02`
- **Scenario**: Instance indexer + extension Length on class
- **Expected**: Proper instance/extension member mixing

#### Test: `ImplicitIndexIndexerAccess_CompoundAssignment_03_03`
- **Scenario**: Extension indexer + instance Length on class
- **Expected**: Correct virtual dispatch for instance members

### 1.4 Generic Extension Parameter Tests

#### Test: `ImplicitIndexIndexerAccess_CompoundAssignment_04`
- **Scenario**: Unconstrained generic `T`, tested with struct `S1`
- **Variations**: Unconstrained and struct-constrained type parameters
- **Expected**: Boxing/unboxing handling for value types
- **Async Test**: Included (error case - reference cannot cross await boundary)
- **Edge Cases**:
  - Non-variable `default(T)` rejected with CS0131
  - `in T`/`ref readonly T` constraints → CS9301 (must be concrete value type)

#### Test: `ImplicitIndexIndexerAccess_CompoundAssignment_05`
- **Scenario**: Struct-constrained `ref T` extension parameter
- **Expected**: Proper by-ref handling with generics
- **Async Test**: Error CS8178 (ref return cannot cross await)
- **Edge Cases**:
  - `default(T)` with `ref` → CS1510 (twice)
  - `in T`/`ref readonly T` → CS9301

#### Test: `ImplicitIndexIndexerAccess_CompoundAssignment_06`
- **Scenario**: Unconstrained `T` with class value (C1)
- **Variations**: Unconstrained and class-constrained type parameters
- **Expected**: Reference type handling in generic context
- **Async Test**: Error CS8178

### 1.5 Readonly Receiver Tests

#### Test: `ImplicitIndexIndexerAccess_CompoundAssignment_ReadonlyReceiver_040`
- **Scenario**: Unconstrained `T`, readonly field receiver
- **Expected**: Uses unsafe code to modify readonly field for testing purposes
- **Note**: Validates that readonly field is re-read correctly after async

#### Test: `ImplicitIndexIndexerAccess_CompoundAssignment_ReadonlyReceiver_041` (Theory: ref, ref readonly, in)
- **Scenario**: Unconstrained `T` with ref/ref readonly/in parameter
- **Expected**: 
  - `ref`: Mutable access, proper evaluation
  - `ref readonly`/`in`: Copy made, side effects differ
- **IL Verification**: Different local variable patterns for each ref kind

#### Test: `ImplicitIndexIndexerAccess_CompoundAssignment_ReadonlyReceiver_061` (Theory: ref, ref readonly, in)
- **Scenario**: Unconstrained `T` with class value (C1)
- **Expected**: Class receivers don't require copying
- **IL Verification**: Simpler code generation for reference types

---

## 2. Implicit Range Indexer Access - Compound Assignment

### 2.1 Struct Extension Parameter Tests

#### Test: `ImplicitRangeIndexerAccess_CompoundAssignment_01`
- **Scenario**: Extension Length + ref-returning extension Slice, range `1..^1`
- **Expected**: Range decomposed to start/length, Slice called, ref returned
- **IL Verification**: Range.Start, Range.End, Index.GetOffset calls

#### Test: `ImplicitRangeIndexerAccess_CompoundAssignment_01_02`
- **Scenario**: Instance Length + extension Slice
- **Expected**: Proper mixing of instance/extension members

#### Test: `ImplicitRangeIndexerAccess_CompoundAssignment_01_03`
- **Scenario**: Extension Length + instance Slice
- **Expected**: Instance Slice called correctly

### 2.2 Ref/In/Ref Readonly Extension Parameter Tests

#### Test: `ImplicitRangeIndexerAccess_CompoundAssignment_02` (Theory: ref, ref readonly, in)
- **Scenario**: Ref struct with extension Length + Slice
- **Edge Cases**:
  - `ref` → CS1510 for non-variable receiver (twice)
  - `ref readonly` → WRN_RefReadonlyNotVariable (3 warnings)
  - `in` → No errors (copies allowed)

#### Test: `ImplicitRangeIndexerAccess_CompoundAssignment_02_02` (Theory: ref, ref readonly, in)
- **Scenario**: Instance Length + extension Slice, ref struct
- **Expected**: Similar error patterns

#### Test: `ImplicitRangeIndexerAccess_CompoundAssignment_02_03` (Theory: ref, ref readonly, in)
- **Scenario**: Extension Length + instance Slice, ref struct
- **Expected**: Instance method called on copied struct for `in`/`ref readonly`

### 2.3 Class Extension Parameter Tests

#### Test: `ImplicitRangeIndexerAccess_CompoundAssignment_03`
- **Scenario**: Class receiver with state mutations
- **Expected**: Receiver captured once

#### Test: `ImplicitRangeIndexerAccess_CompoundAssignment_03_02`
- **Scenario**: Instance Length + extension Slice on class
- **Expected**: Virtual dispatch for instance members

#### Test: `ImplicitRangeIndexerAccess_CompoundAssignment_03_03`
- **Scenario**: Extension Length + instance Slice on class
- **Expected**: Proper virtual/extension mixing

### 2.4 Generic Extension Parameter Tests

#### Test: `ImplicitRangeIndexerAccess_CompoundAssignment_04`
- **Scenario**: Unconstrained `T` with struct value (S1)
- **Async Test**: Error CS8178 (ref return cannot cross await)
- **Edge Cases**: Non-variable receiver rejected, `in T`/`ref readonly T` → CS9301

#### Test: `ImplicitRangeIndexerAccess_CompoundAssignment_05`
- **Scenario**: Struct-constrained `ref T`
- **Async Test**: Error CS8178
- **Edge Cases**: Same as test 04

#### Test: `ImplicitRangeIndexerAccess_CompoundAssignment_06`
- **Scenario**: Unconstrained `T` with class value (C1)
- **Async Test**: Error CS8178

### 2.5 Readonly Receiver Tests

#### Test: `ImplicitRangeIndexerAccess_CompoundAssignment_ReadonlyReceiver_040`
- **Scenario**: Readonly field with unsafe modification for testing
- **Expected**: Validates proper readonly field handling

#### Test: `ImplicitRangeIndexerAccess_CompoundAssignment_ReadonlyReceiver_041` (Theory: ref, ref readonly, in)
- **Scenario**: Unconstrained `T`, struct value, ref parameters
- **Expected**: Evaluation order differences for `ref` vs `ref readonly`/`in`

#### Test: `ImplicitRangeIndexerAccess_CompoundAssignment_ReadonlyReceiver_061` (Theory: ref, ref readonly, in)
- **Scenario**: Unconstrained `T`, class value
- **Expected**: Simpler code generation for reference types

---

## 3. From-End Index Expression Tests

### 3.1 `^GetInt()` Pattern

#### Test: `ImplicitIndexIndexerAccess_CompoundAssignment_FromEndIndexExpr_01`
- **Scenario**: Compound assignment with `^GetInt()` syntax
- **Expected**: GetInt() evaluated, subtracted from Length
- **IL Verification**: Subtraction of index from length

#### Test: `ImplicitIndexIndexerAccess_CompoundAssignment_ConversionFromIntIndexExpr_01`
- **Scenario**: Explicit cast `(Index)GetInt()`
- **Expected**: No implicit length subtraction
- **IL Verification**: Direct index use without Length call

#### Test: `ImplicitIndexIndexerAccess_CompoundAssignment_ConversionFromIntIndexExpr_02` (Theory: ref, ref readonly, in)
- **Scenario**: Ref struct with `(Index)GetInt()` cast
- **Expected**: Proper by-ref handling

#### Test: `ImplicitIndexIndexerAccess_CompoundAssignment_ConversionFromIntIndexExpr_03`
- **Scenario**: Class receiver with explicit Index cast
- **Expected**: Proper evaluation order

---

## 4. Prefix Increment Assignment (`++x[i]`)

### 4.1 Basic Tests

#### Test: `ImplicitIndexIndexerAccess_PrefixIncrementAssignment_01`
- **Scenario**: Struct extension with `++F[^1]`
- **Expected**: Get, increment, Set in correct order
- **IL Verification**: Temp variable for incremented value

#### Test: `ImplicitIndexIndexerAccess_PrefixIncrementAssignment_02` (Theory: ref, ref readonly, in)
- **Scenario**: Ref struct tests
- **Expected**: Proper by-ref handling

#### Test: `ImplicitIndexIndexerAccess_PrefixIncrementAssignment_03`
- **Scenario**: Class receiver
- **Expected**: Receiver captured once

### 4.2 Readonly Receiver Tests

#### Test: `ImplicitIndexIndexerAccess_PrefixIncrementAssignment_ReadonlyReceiver_041` (Theory: ref, ref readonly, in)
- **Scenario**: Generic `T` with ref parameters
- **Expected**: Proper evaluation with readonly receivers

---

## 5. Postfix Increment Assignment (`x[i]++`)

### 5.1 Basic Tests

#### Test: `ImplicitIndexIndexerAccess_PostfixIncrementAssignment_01`
- **Scenario**: Struct extension with `F[^1]++`
- **Expected**: Get, increment, Set; returns original value
- **IL Verification**: Two temp variables (original and incremented)

#### Test: `ImplicitIndexIndexerAccess_PostfixIncrementAssignment_02` (Theory: ref, ref readonly, in)
- **Scenario**: Ref struct tests
- **Expected**: Proper by-ref handling

#### Test: `ImplicitIndexIndexerAccess_PostfixIncrementAssignment_03`
- **Scenario**: Class receiver
- **Expected**: Receiver captured once

### 5.2 Readonly Receiver Tests

#### Test: `ImplicitIndexIndexerAccess_PostfixIncrementAssignment_ReadonlyReceiver_041` (Theory: ref, ref readonly, in)
- **Scenario**: Generic `T` with ref parameters
- **Expected**: Proper evaluation with readonly receivers

---

## 6. Null-Coalescing Assignment (`x[i] ??= value`)

### 6.1 Basic Tests

#### Test: `ImplicitIndexIndexerAccess_ConditionalAssignment_01`
- **Scenario**: Nullable result type `string?`, compound `??=`
- **Expected**: Get, null check, conditional Set
- **IL Verification**: Branch on null check

#### Test: `ImplicitIndexIndexerAccess_ConditionalAssignment_02` (Theory: ref, ref readonly, in)
- **Scenario**: Ref struct with nullable indexer
- **Expected**: Proper by-ref handling

#### Test: `ImplicitIndexIndexerAccess_ConditionalAssignment_03`
- **Scenario**: Class receiver with nullable indexer
- **Expected**: Receiver captured once

---

## 7. Deconstruction Assignment

### 7.1 Basic Tests

#### Test: `ImplicitIndexIndexerAccess_DeconstructAssignment_01`
- **Scenario**: Tuple deconstruction `(F[^1], int y) = GetTuple()`
- **Expected**: Indexer set via deconstruction target
- **IL Verification**: Tuple unpacking then indexer set

#### Test: `ImplicitIndexIndexerAccess_DeconstructAssignment_02`
- **Scenario**: By-ref struct (must be `ref`, not `in`/`ref readonly`)
- **Expected**: Proper by-ref handling
- **Edge Case**: `in`/`ref readonly` rejected (CS8332 - readonly receiver)

#### Test: `ImplicitIndexIndexerAccess_DeconstructAssignment_03`
- **Scenario**: Class receiver deconstruction
- **Expected**: Receiver captured once

---

## 8. Range Pattern Assignment Operations

### 8.1 Prefix/Postfix Increment with Range

#### Test: `ImplicitRangeIndexerAccess_PrefixIncrementAssignment_01`
- **Scenario**: `++F[1..^1]` with ref-returning Slice
- **Expected**: Slice returns ref, ref incremented

#### Test: `ImplicitRangeIndexerAccess_PostfixIncrementAssignment_01`
- **Scenario**: `F[1..^1]++` with ref-returning Slice
- **Expected**: Original value captured, then incremented

### 8.2 Null-Coalescing with Range

#### Test: `ImplicitRangeIndexerAccess_ConditionalAssignment_01`
- **Scenario**: `F[1..^1] ??= value` with nullable ref-returning Slice
- **Expected**: Slice returns ref, null checked, conditionally assigned

### 8.3 Deconstruction with Range

#### Test: `ImplicitRangeIndexerAccess_DeconstructAssignment_01`
- **Scenario**: `(F[1..^1], int y) = GetTuple()` with ref-returning Slice
- **Expected**: Slice ref used as deconstruction target

---

## Key Test Patterns

### Evaluation Order Verification
- All tests validate that evaluation order matches language specification
- Side effects (field mutations) used to track evaluation order
- IL verification confirms expected evaluation patterns

### Receiver Handling
- **Struct by-value**: Copied for each member access
- **Struct by-ref**: Single address used throughout
- **Struct by-ref readonly/in**: Copy made for non-readonly receivers
- **Class**: Single load at start, reference preserved

### Error Cases
- Non-variable receivers with mutable operations
- Ref returns crossing async boundaries
- `in`/`ref readonly` generic constraints on non-concrete types
- Readonly receivers with assignment operations

### IL Verification
- Local variable usage patterns
- Call instruction types (call vs callvirt)
- Boxing/unboxing for generic value types
- Address-of operations for by-ref access

### Edge Cases
- Unsafe readonly field modification (for testing readonly behavior)
- Nullable reference/value type interactions
- Generic type parameter constraints
- Mixed instance/extension member resolution

