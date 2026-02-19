using Microsoft.Extensions.Compliance.Classification;

namespace Shared.Logging
{
    public static class DataTaxonomy
    {
        public static string TaxonomyName => typeof(DataTaxonomy).FullName!;

        public static readonly DataClassification PersonalData = new(TaxonomyName, nameof(DataTaxonomy.PersonalData));
    }
}
