#if MEMORY_CHECK
using MonoBehaviour = MemoryMonoBehaviour;
#endif
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System.Collections.Generic;

namespace BF
{
    public class StateRoot : MonoBehaviour
    {
        public List<SR.Element> elements; // 元素列表

        static SR.StateConfig[] Empty = new SR.StateConfig[0];

        public SR.StateConfig[] States = Empty; // 状态名

        // 是否允许过渡播放
        bool isSmooth_ = true;
        public bool isSmooth { get { return isSmooth_; } set { isSmooth_ = value; } }

        [SerializeField]
        Button uButton;

        [SerializeField]
        public bool isSetStart = false; // Start是否设置

        // 点击是否切换状态
        public bool isClickSwitchState = false;

        public Button uCurrentButton
        {
            get { return uButton; }
            set
            {
                if (uButton == value)
                    return;

                if (uButton != null)
                {
                    uButton.onClick.RemoveListener(OnButtonClick);
                }

                uButton = value;

#if UNITY_EDITOR
                if (!Application.isPlaying)
                    return;
#endif
                BindButtonEvent();
            }
        }

        void BindButtonEvent()
        {
            if (uButton != null)
            {
                uButton.onClick.AddListener(OnButtonClick);
            }
        }

        void OnButtonClick()
        {
            if (isClickSwitchState)
            {
                if (!NextState())
                {
                    SetState(0);
                }
            }

            onClick.Invoke();
        }

        public bool NextState()
        {
            var value = CurrentState + 1;
            if (hasState(value))
                return SetState(value);

            return false;
        }

        public bool FrontState()
        {
            var value = CurrentState - 1;
            if (hasState(value))
                return SetState(value);

            return false;
        }

        public void SetNextStateWithLoop(bool isNotify = false)
        {
            if (hasState(CurrentState + 1))
            {
                SetCurrentState(CurrentState + 1, isNotify);
            }
            else
            {
                SetCurrentState(0, isNotify);
            }
        }

        public void SetFrontStateWithLoop(bool isNotify = false)
        {
            if (hasState(CurrentState - 1))
            {
                SetCurrentState(CurrentState - 1, isNotify);
            }
            else
            {
                SetCurrentState(States.Length - 1, isNotify);
            }
        }

        public UnityEvent onStateChange = new UnityEvent();
        public UnityEvent onClick = new UnityEvent();

#if UNITY_EDITOR
        [EditorField]
        public string[] StateNames
        {
            get
            {
                string[] s = new string[States.Length];
                for (int i = 0; i < s.Length; ++i)
                {
                    s[i] = States[i].Name;
                }

                return s;
            }
        }

        static void Swap<T>(ref T x, ref T y)
        {
            T t = x;
            x = y;
            y = t;
        }

        // 切换状态顺序
        [EditorField]
        public void SwapState(int x, int y)
        {
            Swap(ref States[x], ref States[y]);
            foreach (var ator in elements)
            {
                Swap(ref ator.stateData[x], ref ator.stateData[y]);
            }
        }
#endif

        [SerializeField]
        int StateIndex; // 当前状态

        protected virtual void Awake()
        {
            BindButtonEvent();
        }

        private void Start()
        {
            if (isSetStart)
                SetState(CurrentState);
        }

        public int CurrentState
        {
            get { return StateIndex; }
            set
            {
                if (CurrentState == value)
                    return;

                SetState(value);
            }
        }

        public string CurrentStateName
        {
            get
            {
                return States[StateIndex].Name;
            }
        }

        public bool hasState(int value)
        {
            if (value < 0 || value >= States.Length)
                return false;
            return true;
        }

        public bool SetCurrentState(int value, bool isnotify)
        {
            if (!hasState(value))
            {
                Log.Error($"StateRoot:{name} {value} count:{States.Length}");
                return false;
            }

            int oldvalue = StateIndex;
            if (oldvalue != value && oldvalue >= 0 && oldvalue < States.Length)
                States[oldvalue].Set(this, 1); // 离开为1状态

            StateIndex = value;
            if (elements != null)
            {
                for (int i = 0; i < elements.Count; ++i)
                {
                    elements[i].Agent.Set(this, elements[i], value);
                }
            }

            States[StateIndex].Set(this, 0); // 进入为0状态

            if (isnotify)
            {
                onStateChange.Invoke();
            }
            return true;
        }

        public bool SetCurrentState(string stateName, bool isnotify = false)
        {
            for (int i = 0; i < States.Length; ++i)
            {
                if (stateName == States[i].Name)
                {
                    StateIndex = i;
                    if (elements != null)
                    {
                        for (int j = 0; j < elements.Count; ++j)
                        {
                            elements[j].Agent.Set(this, elements[j], i);
                        }
                    }

                    if (isnotify)
                    {
                        onStateChange.Invoke();
                    }
                    return true;
                }
            }
            return false;
        }

        bool SetState(int value)
        {
            return SetCurrentState(value, true);
        }

#if UNITY_EDITOR
        [EditorField]
        public void AddElement(SR.Type type, SR.StateConfig sc)
        {
            int lenght = sc == null ? States.Length : 2;

            SR.Element element = new SR.Element();
            element.type = type;
            element.stateData = new SR.ElementStateData[lenght];
            for (int i = 0; i < lenght; ++i)
            {
                element.stateData[i] = new SR.ElementStateData();
                element.Agent.Init(this, element, element.stateData[i]);
            }

            if (sc == null)
            {
                if (elements == null)
                    elements = new List<SR.Element>();

                elements.Add(element);
            }
            else
            {
                if (sc.elements == null)
                    sc.elements = new List<SR.Element>();

                sc.elements.Add(element);
            }
        }

        [EditorField]
        public void AddState()
        {
            int lenght = States.Length;
            System.Array.Resize<SR.StateConfig>(ref States, lenght + 1);

            States[lenght] = new SR.StateConfig();
            States[lenght].Name = lenght.ToString();

            if (elements != null)
            {
                for (int i = 0; i < elements.Count; ++i)
                    elements[i].AddState(this);
            }
        }

        [EditorField]
        public void RemoveState(int index)
        {
            Utility.ArrayRemove(ref States, index);
            if (elements != null)
            {
                for (int i = 0; i < elements.Count; ++i)
                {
                    elements[i].RemoveState(index);
                }
            }
        }
#endif
    }
}