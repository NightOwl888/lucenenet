using Lucene.Net.Util;
using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.Analysis.Morfologik.TokenAttributes
{
    /// <summary>
    /// Morphosyntactic annotations for surface forms.
    /// </summary>
    /// <seealso cref="IMorphosyntacticTagsAttribute"/>
    public class MorphosyntacticTagsAttribute : Attribute, IMorphosyntacticTagsAttribute
#if FEATURE_CLONEABLE
        , ICloneable
#endif
    {
        /// <summary>Initializes this attribute with no tags</summary>
        public MorphosyntacticTagsAttribute() { }

        /// <summary>
        /// A list of potential tag variants for the current token.
        /// </summary>
        private IList<StringBuilder> tags;

        /// <summary>
        /// Gets or sets the POS tag of the term. If you need a copy of this char sequence, copy
        /// its contents (and clone <see cref="StringBuilder"/>s) because it changes with
        /// each new term to avoid unnecessary memory allocations.
        /// </summary>
        public virtual IList<StringBuilder> Tags
        {
            get => tags;
            set => tags = value;
        }


        public override void Clear()
        {
            tags = null;
        }


        public override bool Equals(object other)
        {
            if (other is IMorphosyntacticTagsAttribute)
            {
                return Equal(this.Tags, ((IMorphosyntacticTagsAttribute)other).Tags);
            }
            return false;
        }

        private bool Equal(object l1, object l2)
        {
            return l1 == null ? (l2 == null) : (l1.Equals(l2));
        }

        public override int GetHashCode()
        {
            return this.tags == null ? 0 : tags.GetHashCode();
        }

        public override void CopyTo(IAttribute target)
        {
            List<StringBuilder> cloned = null;
            if (tags != null)
            {
                cloned = new List<StringBuilder>(tags.Count);
                foreach (StringBuilder b in tags)
                {
                    cloned.Add(new StringBuilder(b.ToString()));
                }
            }
            ((IMorphosyntacticTagsAttribute)target).Tags = cloned;
        }

        public override object Clone()
        {
            MorphosyntacticTagsAttribute cloned = new MorphosyntacticTagsAttribute();
            this.CopyTo(cloned);
            return cloned;
        }

        public override void ReflectWith(IAttributeReflector reflector)
        {
            reflector.Reflect(typeof(MorphosyntacticTagsAttribute), "tags", tags);
        }
    }
}
