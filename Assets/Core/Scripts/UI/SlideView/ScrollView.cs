using System.Collections;
using System.Collections.Generic;
using Gavi.Utility;
using UnityEngine;
using UnityEngine.UI;

namespace Gavi
{
    public class ScrollView : MonoBehaviour
    {
        [Header("--- Settings ---")]
        [SerializeField] private float _topMargin;

        [Header("--- References ---")]
        [SerializeField] private RectTransform _scrollViewTransform;
        [SerializeField] private RectTransform _contentTransform;

        public List<ScrollViewElement> Elements => _elements;
        private List<ScrollViewElement> _elements = new List<ScrollViewElement>();
        private List<float> _elementPositions = new List<float>(); // top margin included, own height not included

        public int ElementCount => Elements.Count;


        public void AddElement(ScrollViewElement element, int index = -1)
        {
            bool isValid = element.SetScrollView(this);
            if (!isValid)
                return;

            if (index < 0)
                index = Elements.Count;

            // Vector3 prevLossyScale = element.RectTransform.lossyScale;
            element.RectTransform.SetParent(_contentTransform);
            element.RectTransform.localScale = Vector3.one;
            // Vector3 newLocalScale = element.transform.localScale;
            // Vector3 lossyScale = _contentTransform.lossyScale;
            // newLocalScale.x *= lossyScale.x;
            // newLocalScale.z *= lossyScale.z;
            // newLocalScale.y *= lossyScale.y;
            // element.transform.localScale = newLocalScale;

            int elementCount = Elements.Count;
            float newElementHeight = element.Height;
            for (int i = index; i < elementCount + 1; i++)
            {
                // [0]
                // [20]

                // [0, 1]
                // [20, 100]

                // [0, 2, 1]
                // [20, 100, 150]

                if (i == index)
                {
                    if (_elementPositions.Count == 0) // if list is empty
                        _elementPositions.Insert(i, -_topMargin);
                    else // else list is not empty
                    {
                        if (i < Elements.Count) // if element is not inserted at the end
                            _elementPositions.Insert(i, _elementPositions[i] - _topMargin);
                        else // else element is inserted at the end
                            _elementPositions.Insert(i, _elementPositions[i - 1] - _topMargin - Elements[i - 1].Height);
                    }
                    Elements.Insert(i, element);
                }
                else if (i > index)
                {
                    _elementPositions[i] = _elementPositions[i] - _topMargin - newElementHeight;
                }
                Elements[i].transform.localPosition = new Vector3(0, _elementPositions[i] - element.Height * 0.5f, 0);
                Elements[i].transform.position = new Vector3(_scrollViewTransform.position.x /*- _scrollViewTransform.sizeDelta.x * 0.25f*/, Elements[i].transform.position.y, Elements[i].transform.position.z);
            }
        }

        public ScrollViewElement RemoveElement(ScrollViewElement element, bool destroyElement = true)
        {
            if (!Elements.Contains(element))
            {
                Utility.Debugger.LogError("element not in list!");
                return null;
            }
            int index = Elements.IndexOf(element);
            return RemoveAt(index, destroyElement);
        }
        public ScrollViewElement RemoveAt(int index, bool destroyElement = true)
        {
            if (index < 0 || index >= Elements.Count)
            {
                Utility.Debugger.LogError("Index in ScrollView.RemoveElement is corrupt: " + index + ", max " + Elements.Count);
                return null;
            }

            ScrollViewElement element = Elements[index];
            element.SetScrollView(null);
            Elements.RemoveAt(index);
            _elementPositions.RemoveAt(index);
            float removedHeight = element.Height + _topMargin;
            for (int i = index; i < Elements.Count; i++)
            {
                // [0, 1, 2, 3]
                // [20, 50, 100, 170]

                // [0, 2, 3]
                // [20, 50, 120]
                _elementPositions[i] += removedHeight;
                Elements[i].transform.localPosition = new Vector3(0, _elementPositions[i] - element.Height * 0.5f, 0);
                Elements[i].transform.position = new Vector3(_scrollViewTransform.position.x/* + _scrollViewTransform.sizeDelta.x * 0.5f*/, Elements[i].transform.position.y, Elements[i].transform.position.z);
            }

            if (destroyElement)
            {
                Destroy(element.gameObject);
                element = null;
            }

            return element;
        }

        public void ClearElements(bool destroyElements = true)
        {
            for (int i = Elements.Count - 1; i >= 0; i--)
                RemoveAt(i, destroyElements);
        }

        public int IndexOf(ScrollViewElement element)
        {
            if (!Elements.Contains(element))
                return -1;
            return Elements.IndexOf(element);
        }
    }
}