// Lucene version compatibility level 8.2.0
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using System;
using System.Diagnostics;

namespace Lucene.Net.Analysis.Common.Analysis.Miscellaneous
{
    public abstract class ConditionalTokenFilter : TokenFilter
    {
        //public static ConditionalTokenFilter NewAnonymous()

        private enum TokenState
        {
            READING, PREBUFFERING, DELEGATING
        }

        private sealed class OneTimeWrapper : TokenStream
        {
            private readonly ConditionalTokenFilter outerInstance;
            private readonly IOffsetAttribute offsetAtt;
            private readonly IPositionIncrementAttribute posIncAtt;

            public OneTimeWrapper(ConditionalTokenFilter outerInstance, AttributeSource attributeSource)
                        : base(attributeSource)
            {
                this.outerInstance = outerInstance;
                this.offsetAtt = attributeSource.AddAttribute<IOffsetAttribute>();
                this.posIncAtt = attributeSource.AddAttribute<IPositionIncrementAttribute>();
            }

            public override bool IncrementToken()
            {
                if (outerInstance.state == TokenState.PREBUFFERING)
                {
                    if (posIncAtt.PositionIncrement == 0)
                    {
                        outerInstance.adjustPosition = true;
                        posIncAtt.PositionIncrement = 1;
                    }
                    outerInstance.state = TokenState.DELEGATING;
                    return true;
                }
                Debug.Assert(outerInstance.state == TokenState.DELEGATING);
                if (outerInstance.m_input.IncrementToken())
                {
                    if (outerInstance.ShouldFilter())
                    {
                        return true;
                    }
                    outerInstance.endOffset = offsetAtt.EndOffset;
                    outerInstance.bufferedState = CaptureState();
                }
                else
                {
                    outerInstance.exhausted = true;
                }
                return false;
            }

            public override void Reset()
            {
                // clearing attributes etc is done by the parent stream,
                // so must be avoided here
            }

            public override void End()
            {
                // imitate Tokenizer.end() call - endAttributes, set final offset
                if (outerInstance.exhausted)
                {
                    if (outerInstance.endState == null)
                    {
                        outerInstance.m_input.End();
                        outerInstance.endState = CaptureState();
                    }
                    outerInstance.endOffset = offsetAtt.EndOffset;
                }
                //outerInstance.EndAttributes();
                outerInstance.End(); // LUCENENET: EndAttributes() doesn't exist in 4.8.0, so need to change this when we upgrade
                offsetAtt.SetOffset(outerInstance.endOffset, outerInstance.endOffset);
            }
        }

        private readonly TokenStream @delegate;
        private TokenState state = TokenState.READING;
        private bool lastTokenFiltered;
        private State bufferedState = null;
        private bool exhausted;
        private bool adjustPosition;
        private State endState = null;
        private int endOffset;

        private readonly IPositionIncrementAttribute posIncAtt;

        /// <summary>
        /// Create a new <see cref="ConditionalTokenFilter"/>
        /// </summary>
        /// <param name="input">The input <see cref="TokenStream"/>.</param>
        /// <param name="inputFactory">A factory function to create the wrapped filter(s)</param>
        protected ConditionalTokenFilter(TokenStream input, Func<TokenStream, TokenStream> inputFactory)
                  : base(input)
        {
            this.@delegate = inputFactory.Invoke(new OneTimeWrapper(this, this.m_input));
            posIncAtt = AddAttribute<IPositionIncrementAttribute>();
        }

        /// <summary>
        /// Whether or not to execute the wrapped <see cref="TokenFilter"/>(s) for the current token
        /// </summary>
        protected abstract bool ShouldFilter();


        public override void Reset()
        {
            base.Reset();
            this.@delegate.Reset();
            this.state = TokenState.READING;
            this.lastTokenFiltered = false;
            this.bufferedState = null;
            this.exhausted = false;
            this.adjustPosition = false;
            this.endOffset = -1;
            this.endState = null;
        }

        public override void End()
        {
            if (endState == null)
            {
                base.End();
                endState = CaptureState();
            }
            else
            {
                RestoreState(endState);
            }
            endOffset = GetAttribute<IOffsetAttribute>().EndOffset;
            if (lastTokenFiltered)
            {
                this.@delegate.End();
                endState = CaptureState();
            }
        }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
                this.@delegate.Dispose();
        }


        public override sealed bool IncrementToken()
        {
            lastTokenFiltered = false;
            while (true)
            {
                if (state == TokenState.READING)
                {
                    if (bufferedState != null)
                    {
                        RestoreState(bufferedState);
                        bufferedState = null;
                        lastTokenFiltered = false;
                        return true;
                    }
                    if (exhausted == true)
                    {
                        return false;
                    }
                    if (m_input.IncrementToken() == false)
                    {
                        exhausted = true;
                        return false;
                    }
                    if (ShouldFilter())
                    {
                        lastTokenFiltered = true;
                        state = TokenState.PREBUFFERING;
                        // we determine that the delegate has emitted all the tokens it can at the current
                        // position when OneTimeWrapper.incrementToken() is called in DELEGATING state.  To
                        // signal this back to the delegate, we return false, so we now need to reset it
                        // to ensure that it can continue to emit more tokens
                        @delegate.Reset();
                        bool more = @delegate.IncrementToken();
                        if (more)
                        {
                            state = TokenState.DELEGATING;
                            if (adjustPosition)
                            {
                                int posInc = posIncAtt.PositionIncrement;
                                posIncAtt.PositionIncrement = (posInc - 1);
                            }
                            adjustPosition = false;
                        }
                        else
                        {
                            state = TokenState.READING;
                            return EndDelegating();
                        }
                        return true;
                    }
                    return true;
                }
                if (state == TokenState.DELEGATING)
                {
                    lastTokenFiltered = true;
                    if (@delegate.IncrementToken())
                    {
                        return true;
                    }
                    // no more cached tokens
                    state = TokenState.READING;
                    return EndDelegating();
                }
            }
        }

        private bool EndDelegating()
        {
            if (bufferedState == null)
            {
                Debug.Assert(exhausted == true);
                return false;
            }
            @delegate.End();
            int posInc = posIncAtt.PositionIncrement;
            RestoreState(bufferedState);
            // System.out.println("Buffered posInc: " + posIncAtt.getPositionIncrement() + "   Delegated posInc: " + posInc);
            posIncAtt.PositionIncrement = (posIncAtt.PositionIncrement + posInc);
            if (adjustPosition)
            {
                posIncAtt.PositionIncrement = (posIncAtt.PositionIncrement - 1);
                adjustPosition = false;
            }
            bufferedState = null;
            return true;
        }
    }
}
