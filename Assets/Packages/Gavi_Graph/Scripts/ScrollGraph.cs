using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gavi.Utility;

public class ScrollGraph : GraphBase
{
    // in this kind of graph, if a new value is put into the graph, the latest value is kicked out and the other values are shifted to the left

    #region Member Variables
    [Header("--- Stripes ---")]
    [SerializeField] private int m_stripeDistVertical = 25;
    #endregion

    #region Mono
    void Start()
    {
        if (m_showGraph)
        {
            OnStart();
        }
    }
    #endregion

    #region Init
    protected override void CreateVerticalStripes()
    {
        int numberStripesVertical = (m_maxNumberPoints / (m_stripeDistVertical == 0 ? 1 : m_stripeDistVertical)) + 1;
        for (int i = 0; i < numberStripesVertical; i++)
        {
            GameObject stripesVerticalGameObject = Instantiate(m_lineRendererStripesPrefab, m_stripeLinesTransform);
            stripesVerticalGameObject.name = "Stripe Vertical";
            LineRenderer stripe = stripesVerticalGameObject.GetComponent<LineRenderer>();
            m_stripesVertical.Add(stripe);
        }
    }
    #endregion

    #region Batching
    protected override void AddBatchedValue(float value)
    {
        if (value > m_maxChartValueY)
        {
            ChangeMaxValueY(value);
        }

        // one more for the last point
        if (m_batchedValues.Count > m_maxNumberPoints)
        {
            float oldValue = m_batchedValues[0];
            ShiftValues();
            m_valueLine.SetPosition(m_valueLine.positionCount - 1, new Vector3(m_valueLine.GetPosition(m_valueLine.positionCount - 1).x, ValueToPositionY(value), 0));
            m_batchedValues[m_batchedValues.Count - 1] = value;
            if (oldValue >= m_maxChartValueY - 0.001f)
                ChangeMaxValueY(GetHighestY(ref m_batchedValues));
        }
        else
        {
            m_batchedValues.Add(value);
            Vector3 endPosition = new Vector3((m_batchedValues.Count - 1) / (float)m_maxNumberPoints * m_width, ValueToPositionY(value), 0);
            AddPositionToLine(endPosition);
        }

        UpdateTextCurrent();
        UpdateTextHighesValue();
    }
    #endregion

    #region Position of Stripes/Texts
    protected override void UpdateStripesVertical()
    {
        if (m_stripeDistVertical <= 0)
            return;

        int lastNumber = Mathf.Max(m_maxNumberPoints * m_batchSize, m_numberRawValues);
        int firstNumber = Mathf.Max(0, m_numberRawValues - m_batchSize * m_maxNumberPoints);

        int currentNumber = lastNumber - (lastNumber % (m_stripeDistVertical * m_batchSize));
        for(int i = 0; i < m_stripesVertical.Count; i++)
        {
            if (currentNumber <= firstNumber)
            {
                m_stripesVertical[i].SetPosition(0, Vector3.zero);
                m_stripesVertical[i].SetPosition(1, Vector3.zero);
                m_textsStripesVertical[i].text = "";
                continue;
            }

            float positionPercent = Utility.MapValuePercent(firstNumber, lastNumber, currentNumber);
            // position stripes
            m_stripesVertical[i].SetPosition(0, new Vector3(m_width * positionPercent, 0, 0));
            m_stripesVertical[i].SetPosition(1, new Vector3(m_width * positionPercent, m_height, 0));
            // position texts
            m_textsStripesVertical[i].transform.position = transform.position + m_relativePosition + new Vector3(m_width * positionPercent, 0, 0) + m_textsStripesVerticalRelativePosition;
            m_textsStripesVertical[i].text = currentNumber + "";
            
            currentNumber -= m_stripeDistVertical * m_batchSize;
        }
    }
    #endregion

    #region Shifting
    private void ShiftValues()
    {
        for (int i = 0; i < m_valueLine.positionCount - 1; i++)
        {
            m_valueLine.SetPosition(i, new Vector3(m_valueLine.GetPosition(i).x, m_valueLine.GetPosition(i + 1).y, 0));
            m_batchedValues[i] = m_batchedValues[i + 1];
        }
        UpdateStripesVertical();
    }
    #endregion

}
