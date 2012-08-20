using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegularExpression
{
    class FiniteStateAutomaton
    {
        Deque<State> _states = null;
        public FiniteStateAutomaton()
        {
            _states = new Deque<State>();
        }

        public void PushFront(State state)
        {
            _states.PushFront(state);
        }

        public void PushBack(State state)
        {
            _states.PushBack(state);
        }

        public State GetLastState()
        {
            return _states.PeekBack();
        }

        public State GetFirstState()
        {
            return _states.PeekFront();
        }

        public IEnumerable<State> GetStates()
        {
            foreach (var item in _states)
            {
                yield return item;
            }
        }
    }

    public class RegEx
    {
        private const char EPSILON = '\0';
        private int _nextStateId;
        private Stack<FiniteStateAutomaton> _operandStack;
        private HashSet<char> _inputSet;

        private List<string> _matchedPatterns = new List<string>();
        private List<int> _matchedPatternPositions = new List<int>();
        private string _pattern;
        private List<PatternState> _patternList = new List<PatternState>();
        private int _patternIndex = 0;

        private FiniteStateAutomaton _NFA;
        private FiniteStateAutomaton _DFA;

        public RegEx(string pattern)
        {
            _nextStateId = 0;
            _operandStack = new Stack<FiniteStateAutomaton>();
            _inputSet = new HashSet<char>();

            CreateNFA(pattern);
            ConvertNFAToDFA();
        }

        public Match Match(string pattern)
        {
            _patternList.Clear();
            _pattern = pattern;
            if (Find())
            {
                return new Match(this, _matchedPatternPositions[0], _matchedPatterns[0], true, 1);

            }
            return new Match(null, -1, null, false, -1);
        }

        public bool IsMatch(string pattern)
        {
             _patternList.Clear();
            _pattern = pattern;
            return Find();
        }
        private void CreateNFA(string input)
        {
            Stack<char> operatorStack = new Stack<char>();
            var expandedInput = ConcatExpand(input);
            for (int i = 0; i < expandedInput.Length; i++)
            {
                char c = expandedInput[i];
                if (char.IsLetterOrDigit(c))
                    Push(c);
                else if (operatorStack.Count == 0)
                    operatorStack.Push(c);
                else if (c == '(')
                    operatorStack.Push(c);
                else if (c == ')')
                {
                    while (operatorStack.Peek() != '(')
                    {
                        var op = operatorStack.Pop();
                        if (op == (char)8)
                            Concat();
                        else if (op == '*')
                            Star();
                        else if (op == '|')
                            Union();
                        else
                            return;
                    }
                    operatorStack.Pop(); //pop up '('
                }
                else
                {
                    while (operatorStack.Count != 0 && Presedece(c, operatorStack.Peek()))
                    {
                        var op = operatorStack.Pop();
                        if (op == (char)8)
                            Concat();
                        else if (op == '*')
                            Star();
                        else if (op == '|')
                            Union();
                        else
                            return;
                    }
                    operatorStack.Push(c);
                }
            }

            while (operatorStack.Count > 0)
            {
                var op = operatorStack.Pop();
                if (op == (char)8)
                    Concat();
                else if (op == '*')
                    Star();
                else if (op == '|')
                    Union();
                else
                    return;
            }

            if (_operandStack.Count == 0)
                return;
            var A = _operandStack.Pop();
            A.GetLastState().AcceptState = true;

#if DEBUG
            if (A.GetFirstState() != null)
            {
                using (var stream = File.OpenWrite("NFA.txt"))
                {
                    using (var sw = new StreamWriter(stream))
                    {
                        HashSet<State> states = new HashSet<State>();
                    }
                }
            }
            
#endif
            _NFA = A;
        }

        //Kleens Closure ->Highest
        //Concatenation ->Middle
        //Union        -> lowest
        private bool Presedece(char left, char right)
        {
            if (left == right)
                return true;
            if (left == '*')
                return false;
            if (right == '*')
                return true;
            if (left == (char)8)
                return false;
            if (right == (char)8)
                return true;
            if (left == '|')
                return false;
            return true;

        }

     
        //AB
        private bool Concat()
        {
            if (_operandStack.Count < 2)
                return false;
            FiniteStateAutomaton B = Pop();
            FiniteStateAutomaton A = Pop();

            var state = A.GetLastState();
            state.AddTransition(EPSILON, B.GetFirstState());  //EPSILON states epsilon transition

            foreach (var item in B.GetStates())
            {
                A.PushBack(item);
            }

            _operandStack.Push(A);
            return true;

        }

        //Kleen A*
        // 
        private bool Star()
        {
            if (_operandStack.Count < 1)
                return false;
            FiniteStateAutomaton A = Pop();
            var startState = new State(++_nextStateId);
            var endState = new State(++_nextStateId);
            startState.AddTransition(EPSILON, endState);
            startState.AddTransition(EPSILON, A.GetFirstState());

            A.GetLastState().AddTransition(EPSILON, endState);
            A.GetLastState().AddTransition(EPSILON, A.GetFirstState());
            A.PushBack(endState);
            A.PushFront(startState);

            _operandStack.Push(A);
            return true;
        }

        //A|B
        private bool Union()
        {
            if (_operandStack.Count < 2)
                return false;
            FiniteStateAutomaton B = Pop();
            FiniteStateAutomaton A = Pop();

            var startState = new State(++_nextStateId);
            var endState = new State(++_nextStateId);
            startState.AddTransition(EPSILON, A.GetFirstState());
            startState.AddTransition(EPSILON, B.GetFirstState());

            A.GetLastState().AddTransition(EPSILON, endState);
            B.GetLastState().AddTransition(EPSILON, endState);

            A.PushFront(startState);
            foreach (var item in B.GetStates())
            {
                A.PushBack(item);
            }
            A.PushBack(endState);

            _operandStack.Push(A);
            return true;
        }

        private FiniteStateAutomaton Pop()
        {
            if (_operandStack.Count == 0)
                throw new InvalidOperationException();
            if (_operandStack.Count > 0)
                return _operandStack.Pop();
            return null;
        }

        private void Push(Char input)
        {
            var state0 = new State(++_nextStateId);
            var state1 = new State(++_nextStateId);
            state0.AddTransition(input, state1);

            FiniteStateAutomaton fsa = new FiniteStateAutomaton();
            fsa.PushBack(state0);
            fsa.PushBack(state1);

            _operandStack.Push(fsa);

            _inputSet.Add(input);
        }

        private string ConcatExpand(string input)
        {
            if (input == null)
                return null;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < input.Length; i++)
            {
                if (i > 0 && Char.IsLetterOrDigit(input[i]) && input[i - 1] != '|' && input[i - 1] != '(' && input[i - 1] != ')')
                {
                    sb.Append((Char)8);
                }
                sb.Append(input[i]);
            }
            return sb.ToString();
        }
        
        private HashSet<State> EpsilonClosure(HashSet<State> T)
        {
            var result = new HashSet<State>(T);
            var unprocessedStack = new Stack<State>(T);

            while (unprocessedStack.Count > 0)
            {
                var state = unprocessedStack.Pop();
                var epsilonStates = state.GetStates(EPSILON);
                if (epsilonStates != null)
                {
                    foreach (var item in epsilonStates)
                    {
                        if (!result.Contains(item))
                        {
                            result.Add(item);
                            unprocessedStack.Push(item);
                        }
                    }
                }
            }

            return result;
        }

        private HashSet<State> Move(char input, HashSet<State> T)
        {
            var result = new HashSet<State>();
            foreach (var item in T)
            {
                var states = item.GetStates(input);
                if (states != null)
                {
                    foreach (var state in states)
                    {
                        result.Add(state);
                    }
                }
            }
            return result;
        }
       
        private bool SameStatesSet(HashSet<State> left, HashSet<State> right)
        {
            if (left == null && right == null)
                return true;
            if (left == null || right == null)
                return false;
            if (left.Count != right.Count)
                return false;
            foreach (var item in left)
            {
                if (!right.Contains(item))
                    return false;
            }
            return true;
        }

        private void ConvertNFAToDFA()
        {
            if (_NFA == null)
                return;

            if (_DFA == null)
                _DFA = new FiniteStateAutomaton();
            _nextStateId = 0;
            Stack<State> unmarkedStates = new Stack<State>();
            HashSet<State> DFAStartStateSet = new HashSet<State>();
            HashSet<State> NFAStartStateSet = new HashSet<State>();

            NFAStartStateSet.Add(_NFA.GetFirstState());
            DFAStartStateSet = EpsilonClosure(NFAStartStateSet);

            var DFAStartState = new State(++_nextStateId, DFAStartStateSet);
            _DFA.PushBack(DFAStartState);
            unmarkedStates.Push(DFAStartState);
            while (unmarkedStates.Count > 0)
            {
                var processingDFAState = unmarkedStates.Pop();
                State dest = null;
                foreach (var item in _inputSet)
                {
                    var moveStates = Move(item, processingDFAState.NFAStatesSet);
                    var epsilonClosureStats = EpsilonClosure(moveStates);

                    bool found = false;
                    foreach (var s in _DFA.GetStates())
                    {
                        if (SameStatesSet(s.NFAStatesSet, epsilonClosureStats))
                        {
                            found = true;
                            dest = s;
                            break;
                        }
                    }

                    if (!found)
                    {
                        var newState = new State(++_nextStateId, epsilonClosureStats);
                        unmarkedStates.Push(newState);
                        _DFA.PushBack(newState);
                        processingDFAState.AddTransition(item, newState);
                    }
                    else
                    {
                        processingDFAState.AddTransition(item, dest);
                    }
                }
            }
        }
        
        internal Match Run(int startIndex)
        {

            if (startIndex < _matchedPatternPositions.Count)
            {
                return new Match(this, _matchedPatternPositions[startIndex], _matchedPatterns[startIndex], true, ++startIndex);
            }
            return new Match(null, -1, null, false, -1);
        }

        private bool Find()
        {
            _matchedPatterns.Clear();
            _matchedPatternPositions.Clear();

            for (int j = 0; j < _pattern.Length; ++j)
            {
                char c = _pattern[j];
                for (int i = 0; i < _patternList.Count; i++)
                {
                    var patternState = _patternList[i];
                    var s = patternState.State.GetStates(c);
                    if (s != null && s.Count > 0)
                    {
                        patternState.State = s[0];
                        if (s[0].AcceptState)
                        {
                            _matchedPatternPositions.Add(patternState.StateIndex);
                            _matchedPatterns.Add(_pattern.Substring(patternState.StateIndex, j - patternState.StateIndex + 1));
                        }
                    }
                    else
                    {
                        _patternList.RemoveAt(i);
                        --i;
                    }
                }

                var startState = _DFA.GetFirstState();
                var transition = startState.GetStates(c);
                if (transition != null && transition.Count > 0)
                {
                    var patternState = new PatternState();
                    patternState.StateIndex = j;
                    patternState.State = transition[0];
                    _patternList.Add(patternState);

                    if (transition[0].AcceptState)
                    {
                        _matchedPatternPositions.Add(j);
                        _matchedPatterns.Add(c.ToString());
                    }
                }
                else
                {
                    //then entry state is already accepting state, like a*
                    if (startState.AcceptState)
                    {
                        _matchedPatternPositions.Add(j);
                        _matchedPatterns.Add(c.ToString());
                    }
                }
            }
            return _matchedPatternPositions.Count > 0;
        }
    }

    public class State
    {
        private int _stateId;
        Dictionary<char, List<State>> _destinations;
        private bool _acceptState;
        HashSet<State> _nfaStates;

        public State(int stateId)
            : this(stateId, null)
        {
        }

        public State(int stateId, HashSet<State> nfaStatesSet)
        {
            _stateId = stateId;
            _acceptState = false;
            _destinations = new Dictionary<char, List<State>>();
            _nfaStates = nfaStatesSet;
            

            
            if (_nfaStates != null)
            {
                foreach (var item in _nfaStates)
                {
                    if (item.AcceptState)
                    {
                        _acceptState = true;
                        break;
                    }
                }
            }
        }

        public bool AcceptState
        {
            get
            {
                return _acceptState;
            }
            set
            {
                _acceptState = value;
            }
        }

        public HashSet<State> NFAStatesSet
        {
            get
            {
                return _nfaStates;
            }
        }

        public bool Marked
        {
            get;
            set;
        }
        public int StateId
        {
            get
            {
                return _stateId;
            }
        }

        public List<Char> GetLabels()
        {
            return _destinations.Keys.ToList();
        }

        public bool ContainsLabel(char input)
        {
            if (_destinations == null)
                return false;
            return _destinations.ContainsKey(input);
        }

        public List<State> GetStates(char label)
        {
            if (_destinations.ContainsKey(label))
                return _destinations[label];
            return null;
        }

        public void AddTransition(char input, State destination)
        {
            if (!_destinations.ContainsKey(input))
                _destinations[input] = new List<State>();
            _destinations[input].Add(destination);
        }

        public override int GetHashCode()
        {
            return _stateId.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is State)
            {
                return (obj as State).StateId == this.StateId;
            }
            return false;
        }
        public override string ToString()
        {
            return "State: " + StateId;
        }
    }

    internal class PatternState
    {
        private State _state;
        private int _startIndex;

        public PatternState()
        {
            _state = null;
            _startIndex = -1;
        }

        public PatternState(PatternState other)
        {
            _state = other.State;
            _startIndex = other.StateIndex;
        }

        public State State
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;
            }
        }

        public int StateIndex
        {
            get
            {
                return _startIndex;
            }
            set
            {
                _startIndex = value;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is PatternState)
            {
                var other = obj as PatternState;
                return other.State.Equals(this.State) && other.StateIndex == this.StateIndex;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return _startIndex.GetHashCode() << 8 ^ _state.GetHashCode();
        }
    }


    public class Match
    {
        private RegEx _regex;
        private int _idnex;
        internal Match(RegEx regex, int positon,string value,bool success,int index)
        {
            this.Position = positon;
            this.Value = value;
            this.Success = success;

            this._regex = regex;
            this._idnex = index;
        }
        public int Position { get; set; }
        public string Value { get; set; }
        public bool Success { get; set; }

        public Match NextMatch()
        {
            if (_regex == null)
                return null;
            return _regex.Run(_idnex);
        }

    }
}
