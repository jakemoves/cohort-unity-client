using ShowGraphSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class CueReferenceEnumerator : IEnumerator<CueReference>
{
    private readonly IList<CueReference> _cues;
    private int position = -1;
    
    public CueReference Current
    {  
        get
        {
            try
            {
                return _cues[position];
            }
            catch (IndexOutOfRangeException)
            {
                // TODO: Invalid Operation Exception Message
                throw new InvalidOperationException();
            }
        }
    }

    public bool AtEnd => position >= _cues.Count;
    public bool AtBeginning => position <= -1;

    object IEnumerator.Current => Current;

    public CueReferenceEnumerator(IList<CueReference> cueReferences) 
        => _cues = cueReferences;

    public bool MoveNext()
    {
        if (position < _cues.Count)
            position++;
        return position < _cues.Count;
    }

    public bool MovePrevious()
    {
        if (position > -1)
            position--;
        return position > -1;
    }

    public void Reset()
    {
        position = -1;
    }
    void IDisposable.Dispose() { }
}
