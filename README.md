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

<!-- RANDOM_BENCHMARKS_START -->
| Random | NextBool | Next | NextUInt | NextFloat | NextDouble | NextUint - Range | NextInt - Range |
| ------ | -------- | ---- | -------- | --------- | ---------- | ---------------- | --------------- |
| DotNetRandom | 29,600,000 | 29,600,000 | 31,400,000 | 25,100,000 | 25,600,000 |19,200,000 |18,300,000 |
| LinearCongruentialGenerator | 170,500,000 | 170,600,000 | 242,100,000 | 88,600,000 | 89,100,000 |42,400,000 |39,200,000 |
| IllusionFlow | 143,600,000 | 143,400,000 | 190,600,000 | 80,600,000 | 81,100,000 |40,200,000 |37,100,000 |
| PcgRandom | 160,200,000 | 161,900,000 | 224,000,000 | 85,900,000 | 86,600,000 |41,900,000 |38,100,000 |
| RomuDuo | 128,400,000 | 128,400,000 | 166,100,000 | 76,000,000 | 76,300,000 |39,200,000 |35,600,000 |
| SplitMix64 | 156,700,000 | 157,100,000 | 214,500,000 | 84,900,000 | 84,700,000 |41,600,000 |38,200,000 |
| SquirrelRandom | 123,200,000 | 123,200,000 | 156,800,000 | 73,800,000 | 74,300,000 |38,700,000 |35,400,000 |
| SystemRandom | 74,800,000 | 85,800,000 | 36,000,000 | 67,400,000 | 67,000,000 |37,300,000 |34,200,000 |
| UnityRandom | 50,400,000 | 49,400,000 | 55,500,000 | 38,400,000 | 39,500,000 |26,400,000 |24,900,000 |
| WyRandom | 74,200,000 | 74,900,000 | 84,800,000 | 52,100,000 | 53,100,000 |32,000,000 |29,400,000 |
| XorShiftRandom | 171,800,000 | 172,200,000 | 245,700,000 | 90,000,000 | 89,900,000 |42,400,000 |38,900,000 |
| XoroShiroRandom | 98,700,000 | 98,600,000 | 118,500,000 | 64,200,000 | 64,900,000 |35,100,000 |33,200,000 |
<!-- RANDOM_BENCHMARKS_END -->

# Spatial Trees
There are three implemented 2D immutable spatial trees that can store generic objects, as long as there is some resolution function that can convert them into Vector2 spatial positions.

- QuadTree2D (easiest to use)
- KDTree2D
- RTree2D

Spatial trees, after construction, allow for O(log(n)) spatial query time instead of O(n). They are extremely useful if you need repeated spatial queries, or if you have relatively static spatial data.

## Performance Benchmarks

<!-- SPATIAL_TREE_BENCHMARKS_START -->
#### Construction
| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 1 million points | 2 (0.385s) | 3 (0.330s) | 2 (0.383s) | 0 (1.263s) |

#### Elements In Range
| Elements In Range | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (r=500) | 23 | 23 | 23 | 3 |
| Half (r=250) | 92 | 92 | 82 | 11 |
| Quarter (r=125) | 363 | 364 | 307 | 45 |
| Tiny (r=1) | 33,118 | 34,041 | 43,673 | 27,229 |

#### Get Elements In Bounds
| Get Elements In Bounds | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (size≈dataset) | 128 | 131 | 121 | 5 |
| Half (size≈dataset/2) | 560 | 560 | 353 | 21 |
| Quarter (size≈dataset/4) | 2,194 | 2,191 | 1,052 | 86 |
| Unit (size=1) | 41,744 | 42,016 | 51,974 | 27,981 |

#### Approximate Nearest Neighbors
| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 500 neighbors | 1,123 | 2,167 | 1,610 | 22,501 |
| 100 neighbors | 12,447 | 11,714 | 11,683 | 62,249 |
| 10 neighbors | 142,155 | 122,226 | 83,178 | 104,394 |
| 1 neighbor | 192,116 | 249,394 | 105,872 | 112,233 |
<!-- SPATIAL_TREE_BENCHMARKS_END -->

## Usage

```csharp
GameObject [] spatialStorage = { myCoolGameObject };
QuadTree2D<GameObject> quadTree = new(spatialStorage, go => go.transform.position);

// Might return your object, might not
List<GameObject> inBounds = new();
quadTree.GetElementsInBounds(new Bounds(0, 0, 100, 100), inBounds);

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

