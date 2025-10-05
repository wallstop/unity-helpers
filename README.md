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
| DotNetRandom | 54,900,000 | 55,400,000 | 60,100,000 | 47,500,000 | 48,000,000 |32,900,000 |32,200,000 |
| LinearCongruentialGenerator | 866,600,000 | 866,500,000 | 1,310,200,000 | 186,900,000 | 182,300,000 |67,000,000 |65,400,000 |
| IllusionFlow | 643,700,000 | 643,900,000 | 870,500,000 | 181,000,000 | 176,200,000 |66,300,000 |64,200,000 |
| PcgRandom | 670,300,000 | 672,500,000 | 896,900,000 | 186,600,000 | 181,400,000 |67,000,000 |65,600,000 |
| RomuDuo | 877,100,000 | 812,200,000 | 1,170,700,000 | 188,600,000 | 183,200,000 |67,000,000 |64,800,000 |
| SplitMix64 | 752,200,000 | 761,500,000 | 1,051,800,000 | 188,600,000 | 184,000,000 |67,400,000 |66,000,000 |
| SquirrelRandom | 407,200,000 | 408,400,000 | 413,900,000 | 176,500,000 | 170,800,000 |66,000,000 |64,200,000 |
| SystemRandom | 144,800,000 | 149,200,000 | 64,600,000 | 132,600,000 | 139,700,000 |60,400,000 |57,800,000 |
| UnityRandom | 83,900,000 | 83,900,000 | 86,600,000 | 62,400,000 | 61,700,000 |38,700,000 |38,200,000 |
| WyRandom | 384,300,000 | 384,200,000 | 450,000,000 | 153,100,000 | 165,100,000 |64,600,000 |63,100,000 |
| XorShiftRandom | 756,800,000 | 759,200,000 | 885,400,000 | 188,500,000 | 181,600,000 |67,000,000 |65,500,000 |
| XoroShiroRandom | 740,900,000 | 743,500,000 | 1,063,700,000 | 188,900,000 | 182,600,000 |66,600,000 |65,200,000 |
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
| 1,000,000 entries | 4 (0.247s) | 5 (0.187s) | 3 (0.285s) | 2 (0.348s) |

##### Elements In Range
| Elements In Range | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (~span/2) (r=499.5) | 59 | 58 | 56 | 7 |
| Half (~span/4) (r=249.8) | 237 | 235 | 215 | 27 |
| Quarter (~span/8) (r=124.9) | 946 | 939 | 806 | 117 |
| Tiny (~span/1000) (r=1) | 103,107 | 104,622 | 141,862 | 106,276 |

##### Get Elements In Bounds
| Get Elements In Bounds | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (size=999.0x999.0) | 359 | 387 | 329 | 16 |
| Half (size=499.5x499.5) | 1,854 | 1,848 | 1,217 | 66 |
| Quarter (size=249.8x249.8) | 7,308 | 7,271 | 3,801 | 376 |
| Unit (size=1) | 146,762 | 151,751 | 196,413 | 112,248 |

##### Approximate Nearest Neighbors
| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 500 neighbors | 2,172 | 4,358 | 3,194 | 65,204 |
| 100 neighbors | 24,835 | 23,163 | 24,385 | 157,811 |
| 10 neighbors | 288,446 | 240,205 | 190,653 | 216,205 |
| 1 neighbor | 465,012 | 500,096 | 176,138 | 235,963 |

#### **100,000 entries**

##### Construction
| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 100,000 entries | 50 (0.020s) | 82 (0.012s) | 42 (0.023s) | 46 (0.021s) |

##### Elements In Range
| Elements In Range | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (~span/2) (r=199.5) | 601 | 602 | 593 | 71 |
| Half (~span/4) (r=99.75) | 1,356 | 1,352 | 1,235 | 183 |
| Quarter (~span/8) (r=49.88) | 4,673 | 5,127 | 4,260 | 718 |
| Tiny (~span/1000) (r=1) | 127,735 | 126,876 | 174,736 | 145,721 |

##### Get Elements In Bounds
| Get Elements In Bounds | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (size=399.0x249.0) | 4,561 | 4,626 | 4,566 | 228 |
| Half (size=199.5x124.5) | 9,741 | 11,911 | 7,997 | 970 |
| Quarter (size=99.75x62.25) | 25,768 | 32,226 | 19,597 | 3,800 |
| Unit (size=1) | 184,335 | 183,492 | 238,824 | 154,088 |

##### Approximate Nearest Neighbors
| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 500 neighbors | 2,718 | 2,722 | 2,832 | 65,344 |
| 100 neighbors | 15,190 | 31,000 | 15,124 | 193,287 |
| 10 neighbors | 274,987 | 240,540 | 222,541 | 283,630 |
| 1 neighbor | 325,808 | 489,646 | 254,666 | 287,294 |

#### **10,000 entries**

##### Construction
| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 10,000 entries | 530 (0.002s) | 804 (0.001s) | 546 (0.002s) | 472 (0.002s) |

##### Elements In Range
| Elements In Range | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (~span/2) (r=49.50) | 5,939 | 5,947 | 5,868 | 728 |
| Half (~span/4) (r=24.75) | 22,245 | 22,145 | 13,743 | 2,895 |
| Quarter (~span/8) (r=12.38) | 44,307 | 51,052 | 38,022 | 12,095 |
| Tiny (~span/1000) (r=1) | 166,163 | 161,160 | 233,931 | 166,851 |

##### Get Elements In Bounds
| Get Elements In Bounds | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (size=99.00x99.00) | 45,781 | 46,133 | 46,523 | 2,388 |
| Half (size=49.50x49.50) | 166,089 | 165,865 | 37,609 | 9,233 |
| Quarter (size=24.75x24.75) | 75,042 | 103,207 | 75,726 | 35,182 |
| Unit (size=1) | 239,139 | 231,814 | 318,370 | 176,978 |

##### Approximate Nearest Neighbors
| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 500 neighbors | 3,639 | 3,472 | 3,739 | 59,062 |
| 100 neighbors | 18,238 | 17,125 | 29,690 | 211,150 |
| 10 neighbors | 266,230 | 261,326 | 186,181 | 336,525 |
| 1 neighbor | 481,720 | 556,128 | 283,814 | 384,134 |

#### **1,000 entries**

##### Construction
| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 1,000 entries | 5,336 (0.000s) | 7,246 (0.000s) | 4,835 (0.000s) | 4,426 (0.000s) |

##### Elements In Range
| Elements In Range | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (~span/2) (r=24.50) | 57,414 | 58,063 | 57,299 | 7,367 |
| Half (~span/4) (r=12.25) | 59,828 | 75,859 | 57,017 | 14,660 |
| Quarter (~span/8) (r=6.13) | 94,968 | 107,976 | 95,260 | 37,894 |
| Tiny (~span/1000) (r=1) | 237,548 | 226,698 | 335,919 | 248,125 |

##### Get Elements In Bounds
| Get Elements In Bounds | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (size=49.00x19.00) | 494,282 | 491,324 | 514,053 | 23,938 |
| Half (size=24.50x9.5) | 165,024 | 288,620 | 126,952 | 74,185 |
| Quarter (size=12.25x4.75) | 260,825 | 286,115 | 194,106 | 171,388 |
| Unit (size=1) | 339,194 | 335,318 | 463,648 | 267,899 |

##### Approximate Nearest Neighbors
| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 500 neighbors | 77,160 | 77,849 | 56,308 | 67,167 |
| 100 neighbors | 23,928 | 22,347 | 27,114 | 247,440 |
| 10 neighbors | 432,011 | 422,060 | 236,533 | 427,681 |
| 1 neighbor | 627,691 | 453,725 | 251,426 | 379,354 |

#### **100 entries**

##### Construction
| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 100 entries | 11,273 (0.000s) | 36,630 (0.000s) | 29,850 (0.000s) | 21,551 (0.000s) |

##### Elements In Range
| Elements In Range | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (~span/2) (r=4.5) | 475,823 | 500,595 | 498,910 | 72,771 |
| Half (~span/4) (r=2.25) | 430,457 | 431,471 | 254,456 | 236,793 |
| Quarter (~span/8) (r=1.13) | 430,419 | 431,062 | 589,525 | 339,089 |
| Tiny (~span/1000) (r=1) | 430,842 | 428,426 | 579,176 | 338,411 |

##### Get Elements In Bounds
| Get Elements In Bounds | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (size=9x9) | 2,468,755 | 2,389,191 | 2,490,357 | 222,956 |
| Half (size=4.5x4.5) | 563,312 | 558,511 | 368,042 | 368,222 |
| Quarter (size=2.25x2.25) | 566,003 | 591,958 | 790,906 | 389,098 |
| Unit (size=1) | 586,686 | 594,412 | 788,921 | 368,923 |

##### Approximate Nearest Neighbors
| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 100 neighbors (max) | 224,263 | 222,333 | 273,070 | 296,841 |
| 10 neighbors | 379,243 | 343,914 | 592,798 | 601,512 |
| 1 neighbor | 378,896 | 633,078 | 530,761 | 665,095 |
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

