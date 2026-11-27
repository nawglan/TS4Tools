// Source: legacy_references/Sims4Tools/s4pi/Interfaces/IResourceHandler.cs lines 23-32

namespace s4pi.Interfaces;

/// <summary>
/// Used by WrapperDealer to identify "interesting" classes and the resource types they support.
/// </summary>
/// <remarks>Internal, as in legacy.</remarks>
internal interface IResourceHandler : IDictionary<Type, List<string>>
{
}
