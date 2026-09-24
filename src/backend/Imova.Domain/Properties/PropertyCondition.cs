namespace Imova.Domain.Properties;

// GrayStructure/RedStructure are the Moldovan/Romanian real-estate terms for an unfinished
// building: "variantă albă/sură" (walls/utilities in, no finishing) vs "variantă roșie" (bare
// structure only).
public enum PropertyCondition
{
    New = 1,
    Renovated = 2,
    NeedsRepair = 3,
    GrayStructure = 4,
    RedStructure = 5,
}
