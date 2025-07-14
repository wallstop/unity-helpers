# A Grab-Bag
Various Unity Helpers. Includes many deterministic, seedable random number generators.

# CI/CD Status
![Npm Publish](https://github.com/wallstop/unity-helpers/actions/workflows/npm-publish.yml/badge.svg)

# Compatibility
| Platform | Compatible |
| --- | --- |
| Unity 2021 | Likely, but untested |
| Unity 2022 | &check; |
| Unity 2023 | &check; |
| Unity 6 | &check; |
| URP | &check; |
| HDRP | &check; |

# Installation

## To Install as Unity Package
1. Open Unity Package Manager
2. (Optional) Enable Pre-release packages to get the latest, cutting-edge builds
3. Open the Advanced Package Settings
4. Add an entry for a new "Scoped Registry"
    - Name: `NPM`
    - URL: `https://registry.npmjs.org`
    - Scope(s): `com.wallstop-studios.unity-helpers`
5. Resolve the latest `com.wallstop-studios.unity-helpers`

## From Source
Grab a copy of this repo (either `git clone` or [download a zip of the source](https://github.com/wallstop/unity-helpers/archive/refs/heads/main.zip)) and copy the contents to your project's `Assets` folder.

## From Releases
Check out the latest [Releases](https://github.com/wallstop/unity-helpers/releases) to grab the Unity Package and import to your project.

# Package Contents
- Random Number Generators
- Spatial Trees
- Protobuf, Binary, and JSON formatting
- A resizable CyclicBuffer
- Simple single-threaded thread pool
- A LayeredImage for use with Unity's UI Toolkit
- Geometry Helpers
- Child/Parent/Sibling Attributes to automatically get components
- ReadOnly attribute to disable editing of serialized properties in the inspector
- An extensive collection of helpers
- Simple math functions including a generic Range
- Common buffers to reduce allocations
- A RuntimeSingleton implementation for automatic creation/accessing of singletons
- String helpers, like converting to PascalCase, like Unity does for variable names in the inspector
- A randomizable PerlinNoise implementation
- And more!

# Auto Get(Parent/Sibling/Child)Component
Are you tired of constantly writing GetComponent<T>() all over the place? Waste time no more!

```csharp
public sealed class MyCoolScript : MonoBehaviour
{
    [SiblingComponent] // If it doesn't exist, will log an error
    private SpriteRenderer _spriteRenderer;

    [SiblingComponent(optional = true)] // Ok if it doesn't exist, no errors logged
    private BoxCollider2D _boxCollider;

    [ParentComponent] // Similar to GetComponentInParent<AIController>(includeInactive: true)
    private AIController _parentAIController;

    // Only include components in parents, Unity by default includes sibling components in the GetComponentsInParent call
    [ParentComponent(onlyAncestors = true)] 
    private Transform [] _allParentTransforms; // Works with arrays!

    [ParentComponent(includeInactive = false)] // Don't include inactive components
    private List<PolygonCollider2> _parentColliders; // Works with lists!

    [ChildComponent(onlyDescendents = true)] // Similar to GetComponentInChildren<EdgeCollider2D>(includeInactive: true)
    private EdgeCollider2D _childEdgeCollider;

    private void Awake()
    {
        /*
            Make sure this is called somewhere, usually in Awake, OnEnable, or Start - wherever this is called,
             values will be injected into the annotated fields and errors will be logged
        */
        this.AssignRelationalComponents();
    }
}

```

# Random Number Generators
This package implements several high quality, seedable, and serializable random number generators. The best one (currently) is the PCG Random. This has been hidden behind the `PRNG.Instance` class, which is thread-safe. As the package evolves, the exact implementation of this may change.

All of these implement a custom [IRandom](./Runtime/Core/Random/IRandom.cs) interface with a significantly richer suite of methods than the standard .Net and Unity randoms offer.

To use:

```csharp
IRandom random = PRNG.Instance;

random.NextFloat(); // Something between 0.0f and 1.0f
random.NextDouble(); // Something between 0.0d and 1.0d
random.Next(); // Something between 0 and int.MaxValue
random.NextUint(); // Something between 0 and uint.MaxValue

int [] values = {1, 2, 3};
random.NextOf(values); // 1, 2, or 3
random.NextOf(Enumerable.Range(0, 3)); // 1, 2, or 3
HashSet<int> setValues = new() {1, 2, 3};
random.NextOf(setValues); // 1, 2, or 3

random.NextGuid(); // A valid UUIDv4
random.NextGaussian(); // A value sampled from a gaussian curve around mean=0, stdDev=1 (configurable via parameters)
random.NextEnum<T>(); // A randomly selected enum of type T

int width = 100;
int height = 100;
random.NextNoiseMap(width, height); // A configurable noise map generated using random octave offsets
```

## Implemented Random Number Generators
- PCG
- DotNet (uses the currently implemented .Net Random)
- RomoDuo
- SplitMix64
- Squirrel
- System (uses a port of the Windows .Net Random)
- Unity (uses Unity's random under the hood)
- Wy
- XorShift
- XorShiro

## Performance (Number of Operations / Second)

| Random | NextBool | Next | NextUInt | NextFloat | NextDouble | NextUint - Range | NextInt - Range |
| ------ | -------- | ---- | -------- | --------- | ---------- | ---------------- | --------------- |
| PcgRandom | 166,600,000 | 165,900,000 | 232,300,000 | 89,700,000 | 87,800,000 |42,900,000 |38,900,000 |
| SystemRandom | 76,700,000 | 88,300,000 | 37,200,000 | 69,500,000 | 70,100,000 |38,400,000 |35,200,000 |
| SquirrelRandom | 126,600,000 | 126,200,000 | 161,400,000 | 75,900,000 | 77,100,000 |39,800,000 |36,700,000 |
| XorShiftRandom | 177,300,000 | 178,300,000 | 254,700,000 | 93,100,000 | 92,000,000 |44,300,000 |40,100,000 |
| DotNetRandom | 30,300,000 | 30,500,000 | 32,000,000 | 25,900,000 | 26,300,000 |20,400,000 |18,900,000 |
| WyRandom | 77,300,000 | 77,000,000 | 88,500,000 | 55,300,000 | 55,300,000 |32,900,000 |31,000,000 |
| SplitMix64 | 161,600,000 | 161,100,000 | 222,100,000 | 88,300,000 | 88,300,000 |43,300,000 |39,300,000 |
| RomuDuo | 133,700,000 | 130,500,000 | 172,700,000 | 77,500,000 | 79,500,000 |40,800,000 |37,100,000 |
| XoroShiroRandom | 102,600,000 | 102,300,000 | 124,400,000 | 66,600,000 | 67,500,000 |37,300,000 |34,400,000 |
| UnityRandom | 52,200,000 | 51,700,000 | 56,500,000 | 40,100,000 | 40,300,000 |27,100,000 |25,200,000 |
| GroundZeroRandom | 147,900,000 | 148,500,000 | 197,400,000 | 83,500,000 | 84,400,000 |41,700,000 |38,300,000 |

# Spatial Trees
There are three implemented 2D immutable spatial trees that can store generic objects, as long as there is some resolution function that can convert them into Vector2 spatial positions.

- QuadTree (easiest to use)
- KDTree
- RTree

Spatial trees, after construction, allow for O(log(n)) spatial query time instead of O(n). They are extremely useful if you need repeated spatial queries, or if you have relatively static spatial data.

## Usage

```csharp
GameObject [] spatialStorage = { myCoolGameObject };
QuadTree<GameObject> quadTree = new(spatialStorage, go => go.transform.position);

// Might return your object, might not
GameObject [] inBounds = quadTree.GetElementsInBounds(new Bounds(0, 0, 100, 100));

// Uses a "good-enough" nearest-neighbor approximately for cheap neighbors
List<GameObject> nearestNeighbors = new();
quadTree.GetApproximateNearestNeighbors(myCoolGameObject.transform.position, 1, nearestNeighbors);
Assert.AreEqual(1, nearestNeighbors.Count);
Assert.AreEqual(myCoolGameObject, nearestNeighbors[0]);
```

## Note
All spatial trees expect the positional data to be *immutable*. It is very important that the positions do not change. If they do, you will need to reconstruct the tree.

## Shaders

| Name | Description |
| ---- | ----------- |
| BackgroundMask | Used to simulate a `blur` effect for arbitrary shapes. You will need a reference image, as well as a blurred copy using either a photo editing application or the provided `Image Blur` tool. |
| DebugDisplayValue | Displays numerical values on top of a texture. Useful to visually track texture instances. |

## Contributing

This project uses [CSharpier](https://csharpier.com/) with the default configuration to enable an enforced, consistent style. If you would like to contribute, recommendation is to ensure that changed files are ran through CSharpier prior to merge. This can be done automatically through editor plugins, or, minimally, by installing a [pre-commit hook](https://pre-commit.com/#3-install-the-git-hook-scripts).

If you think there is something useful that you would like to see, please open an issue or contact me directly.