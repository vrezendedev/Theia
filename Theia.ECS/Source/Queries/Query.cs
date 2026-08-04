using System;
using System.Runtime.CompilerServices;
using Theia.ECS.Archetypes;
using Theia.ECS.Assemblages;
using Theia.ECS.Components;
using Theia.ECS.Extensions;
using Theia.ECS.Worlds;

namespace Theia.ECS.Queries;

/// <summary>
/// Base class for every query in Theia ECS. Carries the world reference shared by all query
/// shapes; the per-shape state and iteration surface live on derivatives.
/// </summary>
/// <remarks>
/// <b>User code does not derive from <see cref="Query"/> directly</b>. Choose between
/// <see cref="SettlerQuery"/> for assemblage-bound iteration and <see cref="NomadQuery"/> for
/// composition-based iteration across archetypes.
/// </remarks>
public abstract class Query
{
    protected readonly World _world;

    internal Query(in World world) => _world = world;

    public World GetWorld() => _world;
}

/// <summary>
/// Base class <b>for assemblage-bound queries</b>. Iterates exactly one <see cref="Archetype"/>, the
/// one paired with the supplying <see cref="Assemblage"/>, so iteration skips archetype
/// matching entirely. Use this when every entity to visit was created through a known factory.
/// </summary>
public abstract class SettlerQuery : Query
{
    internal readonly Archetype _archetype;
    private readonly int[] _componentStorageMapping;

    internal SettlerQuery(in World world, in Assemblage assemblage)
        : base(in world)
    {
        _archetype = assemblage._archetype;
        _componentStorageMapping = assemblage._componentStorageMapping;
    }

    /// <summary>
    /// Returns the archetype's component-to-storage-slot mapping cached on the assemblage, used
    /// by typed derivatives to resolve each requested component's storage slot in O(1).
    /// </summary>
    internal ReadOnlySpan<int> GetComponentStorageMapping() => _componentStorageMapping.AsSpan();
}

/// <summary>
/// Base class for <b>composition-based queries</b>. Holds a <see cref="Signature"/> describing the
/// required component set and the dynamically growing list of <see cref="Archetype"/>s whose
/// signatures satisfy it. Matches new archetypes as they appear in the world.
/// </summary>
/// <remarks>
/// Archetype matching is incremental: when the world creates a new archetype, every registered
/// nomad query whose signature is satisfied by the new archetype's gets it appended to its
/// matched-archetypes list. There is no per-iteration match step, <b>iteration is a straight walk
/// over the cached match list</b>.
/// </remarks>
public abstract class NomadQuery : Query
{
    private const int DefaultMatchedArchetypesCapacity = 4;
    private const int DefaultMatchedArchetypesGrowthFactor = 2;

    internal readonly Signature _signature;

    protected int _matchedArchetypesCount;
    private Archetype[] _matchedArchetypes;

    internal NomadQuery(in World world, ReadOnlySpan<int> componentIds)
        : base(in world)
    {
        _signature = new Signature(componentIds);
        _matchedArchetypes = new Archetype[DefaultMatchedArchetypesCapacity];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ReadOnlySpan<Archetype> GetMatchedArchetypes() =>
        _matchedArchetypes.AsSpan(0, _matchedArchetypesCount);

    /// <summary>
    /// Appends <paramref name="archetype"/> to the matched-archetypes list, growing the backing
    /// array as needed. Called by the world when a newly created archetype's signature satisfies
    /// this query's signature.
    /// </summary>
    internal void Add(Archetype archetype)
    {
        int index = _matchedArchetypesCount;

        Array.TryResize(ref _matchedArchetypes, index, DefaultMatchedArchetypesGrowthFactor);

        _matchedArchetypes[index] = archetype;

        _matchedArchetypesCount++;
    }
}
