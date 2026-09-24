using System;
using System.Collections;
using System.Collections.Generic;
using Gavi.Utility;
using UnityEngine;
using UnityEngine.UI;

namespace Gavi
{
    public class TerrainGroupUIShapes : TerrainGroupUI
    {
        public static TerrainGroupUIShapes Instance;
        
        [SerializeField] private ScrollView _scrollView;
        [SerializeField] private ScrollViewElementTerrainShape _prefabScrollViewElement;
        [SerializeField] private Button _buttonAddShapeInitial;

        private void Awake()
        {
            if (Instance != null)
                Debugger.LogInstanceError(GetType());
            Instance = this;
            
            _buttonAddShapeInitial.onClick.AddListener(OnPressedAddInitial);
        }

        public override void ExtractProfile(TerrainSettingsProfile profile)
        {
            base.ExtractProfile(profile);
            if (profile.Shapes == null)
                profile.Shapes = new List<TerrainShapeData>();
            while (profile.Shapes.Count != _scrollView.ElementCount)
            {
                if (profile.Shapes.Count < _scrollView.ElementCount)
                    profile.Shapes.Add(new TerrainShapeData());
                else if (profile.Shapes.Count > _scrollView.ElementCount)
                    profile.Shapes.RemoveAt(profile.Shapes.Count - 1);
            }
            for (int i = 0; i < _scrollView.ElementCount; i++)
                ((ScrollViewElementTerrainShape)_scrollView.Elements[i]).ExtractProfile(profile.Shapes[i]);
        }

        public override void ShowProfile(TerrainSettingsProfile profile)
        {
            base.ShowProfile(profile);
            while (_scrollView.ElementCount != profile.Shapes.Count)
            {
                if (_scrollView.ElementCount < profile.Shapes.Count)
                {
                    _scrollView.AddElement(Instantiate(_prefabScrollViewElement).GetComponent<ScrollViewElementTerrainShape>());
                    _buttonAddShapeInitial.gameObject.SetActive(false);
                }
                else if (_scrollView.ElementCount > profile.Shapes.Count)
                {
                    _scrollView.RemoveAt(_scrollView.ElementCount - 1);
                    if (_scrollView.ElementCount == 0)
                        _buttonAddShapeInitial.gameObject.SetActive(true);
                }
            }
            for (int i = 0; i < _scrollView.ElementCount; i++)
                ((ScrollViewElementTerrainShape)_scrollView.Elements[i]).ShowProfile(profile.Shapes[i]);
        }

        public void OnPressedMoveUp(ScrollViewElementTerrainShape element)
        {
            int index = _scrollView.IndexOf(element);
            if (index <= 0)
                return;
            if (index >= _scrollView.ElementCount)
                return;
            _scrollView.RemoveElement(element, false);
            _scrollView.AddElement(element, index - 1);
        }
        
        public void OnPressedMoveDown(ScrollViewElementTerrainShape element)
        {
            int index = _scrollView.IndexOf(element);
            if (index < 0)
                return;
            if (index >= _scrollView.ElementCount - 1)
                return;
            _scrollView.RemoveElement(element, false);
            _scrollView.AddElement(element, index);
        }
        
        private void OnPressedAddInitial()
        {
            OnPressedAdd(null);
        }
        
        public void OnPressedAdd(ScrollViewElementTerrainShape element)
        {
            ScrollViewElementTerrainShape newElement = Instantiate(_prefabScrollViewElement).GetComponent<ScrollViewElementTerrainShape>();
            int index = element == null ? 0 : _scrollView.IndexOf(element) + 1;
            _scrollView.AddElement(newElement, index);
            _buttonAddShapeInitial.gameObject.SetActive(false);
            OnUIValueChanged();
        }

        public void OnPressedRemove(ScrollViewElementTerrainShape element)
        {
            _scrollView.RemoveElement(element);
            if (_scrollView.ElementCount == 0)
                _buttonAddShapeInitial.gameObject.SetActive(true);
            OnUIValueChanged();
        }

    }
}
