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
There are spatial tree implementations for both 2D and 3D immutable spatial trees that can store generic objects, as long as there is some resolution function that can convert them into spatial positions.

## 2D Spatial Trees
- QuadTree2D (easiest to use)
- KDTree2D
- RTree2D

## 3D Spatial Trees
- OctTree3D (easiest to use)
- KDTree3D
- RTree3D

Spatial trees, after construction, allow for O(log(n)) spatial query time instead of O(n). They are extremely useful if you need repeated spatial queries, or if you have relatively static spatial data.

## 2D Performance Benchmarks

<!-- SPATIAL_TREE_BENCHMARKS_START -->
<!-- tabs:start -->

#### **1,000,000 entries**

##### Construction
| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 1,000,000 entries | 2 (0.386s) | 3 (0.331s) | 2 (0.380s) | 1 (0.525s) |

##### Elements In Range
| Elements In Range | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (~span/2) (r=499.5) | 23 | 23 | 23 | 3 |
| Half (~span/4) (r=249.8) | 91 | 92 | 83 | 12 |
| Quarter (~span/8) (r=124.9) | 363 | 365 | 307 | 49 |
| Tiny (~span/1000) (r=1) | 32,434 | 32,906 | 43,657 | 28,543 |

##### Get Elements In Bounds
| Get Elements In Bounds | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (sizeâ‰ˆ999.0x999.0) | 130 | 131 | 121 | 6 |
| Half (sizeâ‰ˆ499.5x499.5) | 559 | 558 | 351 | 22 |
| Quarter (sizeâ‰ˆ249.8x249.8) | 2,194 | 2,190 | 1,048 | 91 |
| Unit (size=1) | 41,773 | 41,969 | 51,002 | 28,799 |

##### Approximate Nearest Neighbors
| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 500 neighbors | 1,124 | 2,165 | 1,595 | 29,204 |
| 100 neighbors | 12,460 | 11,699 | 11,489 | 69,373 |
| 10 neighbors | 148,506 | 120,511 | 84,998 | 106,643 |
| 1 neighbor | 231,810 | 256,979 | 105,022 | 112,604 |

#### **100,000 entries**

##### Construction
| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 100,000 entries | 31 (0.032s) | 18 (0.053s) | 28 (0.035s) | 25 (0.039s) |

##### Elements In Range
| Elements In Range | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (~span/2) (r=199.5) | 227 | 220 | 224 | 30 |
| Half (~span/4) (r=99.75) | 513 | 516 | 470 | 76 |
| Quarter (~span/8) (r=49.88) | 1,721 | 1,936 | 1,596 | 299 |
| Tiny (~span/1000) (r=1) | 40,716 | 40,683 | 55,432 | 39,310 |

##### Get Elements In Bounds
| Get Elements In Bounds | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (sizeâ‰ˆ399.0x249.0) | 1,408 | 1,408 | 1,407 | 55 |
| Half (sizeâ‰ˆ199.5x124.5) | 2,591 | 3,257 | 2,104 | 240 |
| Quarter (sizeâ‰ˆ99.75x62.25) | 6,835 | 8,255 | 5,018 | 941 |
| Unit (size=1) | 50,145 | 50,474 | 63,587 | 40,075 |

##### Approximate Nearest Neighbors
| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 500 neighbors | 1,393 | 1,389 | 1,445 | 29,539 |
| 100 neighbors | 7,413 | 15,337 | 7,485 | 90,778 |
| 10 neighbors | 173,565 | 123,059 | 97,532 | 126,952 |
| 1 neighbor | 157,201 | 249,884 | 118,993 | 141,347 |

#### **10,000 entries**

##### Construction
| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 10,000 entries | 342 (0.003s) | 385 (0.003s) | 306 (0.003s) | 263 (0.004s) |

##### Elements In Range
| Elements In Range | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (~span/2) (r=49.50) | 2,284 | 2,274 | 2,265 | 306 |
| Half (~span/4) (r=24.75) | 8,623 | 8,613 | 5,126 | 1,195 |
| Quarter (~span/8) (r=12.38) | 15,376 | 17,928 | 13,765 | 4,846 |
| Tiny (~span/1000) (r=1) | 53,814 | 52,189 | 74,073 | 45,527 |

##### Get Elements In Bounds
| Get Elements In Bounds | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (sizeâ‰ˆ99.00x99.00) | 13,995 | 13,995 | 13,983 | 595 |
| Half (sizeâ‰ˆ49.50x49.50) | 43,930 | 43,922 | 8,832 | 2,286 |
| Quarter (sizeâ‰ˆ24.75x24.75) | 18,273 | 25,151 | 18,055 | 8,785 |
| Unit (size=1) | 65,018 | 62,835 | 79,901 | 46,475 |

##### Approximate Nearest Neighbors
| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 500 neighbors | 1,820 | 1,793 | 1,894 | 28,310 |
| 100 neighbors | 9,212 | 8,717 | 14,742 | 94,987 |
| 10 neighbors | 139,126 | 137,797 | 86,347 | 162,371 |
| 1 neighbor | 251,933 | 318,403 | 138,358 | 187,224 |

#### **1,000 entries**

##### Construction
| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 1,000 entries | 3,254 (0.000s) | 3,974 (0.000s) | 2,858 (0.000s) | 2,426 (0.000s) |

##### Elements In Range
| Elements In Range | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (~span/2) (r=24.50) | 22,183 | 21,917 | 21,880 | 3,034 |
| Half (~span/4) (r=12.25) | 21,299 | 27,200 | 20,529 | 5,870 |
| Quarter (~span/8) (r=6.13) | 32,457 | 36,900 | 34,024 | 13,391 |
| Tiny (~span/1000) (r=1) | 78,125 | 78,531 | 114,156 | 71,170 |

##### Get Elements In Bounds
| Get Elements In Bounds | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (sizeâ‰ˆ49.00x19.00) | 132,060 | 132,044 | 132,005 | 5,919 |
| Half (sizeâ‰ˆ24.50x9.5) | 41,790 | 72,326 | 29,756 | 18,865 |
| Quarter (sizeâ‰ˆ12.25x4.75) | 69,263 | 71,766 | 47,022 | 45,112 |
| Unit (size=1) | 92,154 | 88,527 | 111,510 | 74,357 |

##### Approximate Nearest Neighbors
| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 500 neighbors | 33,991 | 34,200 | 25,593 | 29,022 |
| 100 neighbors | 11,767 | 11,455 | 13,356 | 112,930 |
| 10 neighbors | 223,425 | 227,346 | 116,934 | 208,621 |
| 1 neighbor | 367,108 | 240,917 | 122,853 | 241,254 |

#### **100 entries**

##### Construction
| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 100 entries | 24,096 (0.000s) | 22,675 (0.000s) | 17,271 (0.000s) | 12,690 (0.000s) |

##### Elements In Range
| Elements In Range | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (~span/2) (r=4.5) | 195,022 | 194,782 | 192,290 | 29,133 |
| Half (~span/4) (r=2.25) | 149,838 | 149,167 | 97,317 | 78,096 |
| Quarter (~span/8) (r=1.13) | 152,300 | 151,789 | 202,729 | 98,773 |
| Tiny (~span/1000) (r=1) | 152,162 | 151,035 | 202,710 | 98,764 |

##### Get Elements In Bounds
| Get Elements In Bounds | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (sizeâ‰ˆ9x9) | 811,981 | 839,208 | 841,186 | 57,161 |
| Half (sizeâ‰ˆ4.5x4.5) | 139,207 | 135,021 | 89,153 | 98,499 |
| Quarter (sizeâ‰ˆ2.25x2.25) | 148,287 | 148,407 | 205,609 | 105,275 |
| Unit (size=1) | 148,230 | 148,400 | 206,184 | 105,295 |

##### Approximate Nearest Neighbors
| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 100 neighbors (max) | 101,256 | 100,397 | 128,520 | 131,294 |
| 10 neighbors | 200,952 | 186,272 | 306,267 | 326,950 |
| 1 neighbor | 204,858 | 370,816 | 305,530 | 392,533 |
<!-- tabs:end -->
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

## 3D Performance Benchmarks

<!-- SPATIAL_TREE_3D_BENCHMARKS_START -->

<!-- tabs:start -->



#### **1,000,000 entries**



_Run the performance tests to populate these tables._



#### **100,000 entries**



_Run the performance tests to populate these tables._



#### **10,000 entries**



_Run the performance tests to populate these tables._



#### **1,000 entries**



_Run the performance tests to populate these tables._



#### **100 entries**



_Run the performance tests to populate these tables._



<!-- tabs:end -->

<!-- SPATIAL_TREE_3D_BENCHMARKS_END -->

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

