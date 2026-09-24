namespace Imova.Domain.Properties;

// GrayStructure/RedStructure are the Moldovan/Romanian real-estate stages of an unfinished
// building: "variantă sură" (plastered, utilities in, no finishing) vs "la roșu" (bare structure
// only).
public enum PropertyCondition
{
    New = 1,
    Renovated = 2,
    NeedsRepair = 3,
    GrayStructure = 4,
    RedStructure = 5,
}
