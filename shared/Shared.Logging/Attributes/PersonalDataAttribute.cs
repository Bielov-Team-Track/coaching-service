using Microsoft.Extensions.Compliance.Classification;

namespace Shared.Logging.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public class PersonalDataAttribute : DataClassificationAttribute
    {
        public PersonalDataAttribute() : base(DataTaxonomy.PersonalData) { }
    }
}
