using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gavi.Utility;

public class SimpleGraph : GraphBase
{
    // this kind of graph shows all the values put into the graph. The graph is smoothed in x-dimension, once there are too many points in the graph. When smoothing, the
    // values are typically averaged, but can be altered by m_smoothingType.

    #region Member Variables

    [Header("--- Graph ---")]
    [SerializeField][Range(0.1f, 1f)] private float m_initialGapPercent = 1;

    [Header("--- Rescaling / Smothing ---")]
    [SerializeField][Range(0.5f, 0.999f)] private float m_rescaleFactorX = 0.99f;
    [SerializeField] private SmoothingType m_smoothingType;

    [Header("--- Stripes ---")]
    [SerializeField] private List<float> m_stripesVerticalDists;

    // rescaling
    private float m_currentGapPercent;
    private float m_currentPositionXPercent;
    private int m_smoothingStartIndex;

    // value lists
    private List<float> m_rawValues;

    #endregion

    #region Enums
    private enum SmoothingType { deleteEverySecond, average, maximum, minimum, exact, none }
    #endregion

    #region Mono
    void Start ()
    {
        if (m_showGraph)
        {
            m_currentGapPercent = Mathf.Max(m_initialGapPercent, 1 / (m_maxNumberPoints != 0 ? m_maxNumberPoints : 1));
            m_currentPositionXPercent = -m_currentGapPercent;
            OnStart();

        }
    }
    #endregion

    #region Init
    protected override void CreateVerticalStripes()
    {
        for (int i = 0; i < m_stripesVerticalDists.Count; i++)
        {
            GameObject stripeGameObject = Instantiate(m_lineRendererStripesPrefab, m_stripeLinesTransform);
            stripeGameObject.name = "Stripe Vertical " + i;
            LineRenderer stripe = stripeGameObject.GetComponent<LineRenderer>();
            m_stripesVertical.Add(stripe);
        }
    }
    #endregion

    #region Position Control
    protected override void UpdateStripesVertical()
    {
        if (m_stripeCurrentNumber != null)
        {
            int value = m_numberRawValues;
            float positionX = m_width;
            //m_stripeCurrentNumber.SetPosition(0, new Vector3(positionX, 0, 0));
            //m_stripeCurrentNumber.SetPosition(1, new Vector3(positionX, m_height, 0));

            Vector3 position = m_textsTransform.position + new Vector3(positionX, 0, 0) + m_textStripeCurrentNumberRelativePosition;
            m_textCurrentNumber.transform.position = position;
            m_textCurrentNumber.text = "" + value;
        }

        if (m_stripesVerticalDists == null || m_stripesVerticalDists.Count == 0)
            return;

        if (m_numberRawValues < m_stripesVerticalDists[m_stripesVerticalDists.Count - 1])
            return;

        int currentIndex = 0;
        List<float> highestValues = new List<float>(m_stripesVerticalDists);
        while (true)
        {
            float currentValue = highestValues[currentIndex] * 10 + 0.0000000001f;
            highestValues[currentIndex] = currentValue;

            if (currentValue >= m_numberRawValues)
                break;

            currentIndex++;
            if (currentIndex >= m_stripesVerticalDists.Count)
                currentIndex = 0;

        }
        highestValues[currentIndex] = highestValues[currentIndex] / 10 + 0.0000000001f;

        for (int i = 0; i < highestValues.Count; i++)
        {
            int value = (int)highestValues[i];
            float positionX = Utility.MapValuePercent(0, m_numberRawValues, value) * m_width;
            m_stripesVertical[i].SetPosition(0, new Vector3(positionX, 0, 0));
            m_stripesVertical[i].SetPosition(1, new Vector3(positionX, m_height, 0));

            if (m_textsStripesVertical == null || i >= m_textsStripesVertical.Count)
                continue;
            Vector3 position = m_textsTransform.position + new Vector3(positionX, 0, 0) + m_textsStripesVerticalRelativePosition;
            m_textsStripesVertical[i].transform.position = position;
            m_textsStripesVertical[i].text = "" + value;
        }
    }
    #endregion

    #region Batching
    protected override void AddBatchedValue(float value)
    {
        m_batchedValues.Add(value);
        if(value > m_maxChartValueY)
        {
            ChangeMaxValueY(value);
        }

        Vector3 endPosition = new Vector3(GetNextX(), ValueToPositionY(value), 0);
        AddPositionToLine(endPosition);

        SmoothGraph();
        RescaleX();

        UpdateTextCurrent();
        UpdateStripesVertical();
        UpdateTextHighesValue();
    }
    #endregion

    #region Rescaling / Smoothing
    private void RescaleX()
    {
        if(m_currentPositionXPercent < 1 || m_rescaleFactorX <= 0 || m_rescaleFactorX >= 1)
            return;

        float rescaleFactor = Mathf.Min(m_rescaleFactorX, 1 / m_currentPositionXPercent);

        m_currentGapPercent *= rescaleFactor;
        m_currentPositionXPercent *= rescaleFactor;

        for (int i = 0; i < m_valueLine.positionCount; i++)
        {
            m_valueLine.SetPosition(i, new Vector3(m_valueLine.GetPosition(i).x * rescaleFactor/*m_rescaleFactorX*/, m_valueLine.GetPosition(i).y, 0));
        }
    }
    private void SmoothGraph()
    {
        if (m_valueLine.positionCount <= m_maxNumberPoints || m_smoothingType == SmoothingType.none)
            return;

        if (m_maxNumberPoints % 2 != 0)
            Debug.Log("Warning: m_maxNumberPoints should be divisable by 2!");


        bool searchForNewMaxValue = false;
        List<float> newBatchedValues = new List<float>();
        for(int i = 0; i < m_maxNumberPoints - 1; i+=2)
        {
            float newValue = 0;
            float firstValue = m_batchedValues[i];
            float secondValue = m_batchedValues[i+1];

            if (m_smoothingType == SmoothingType.deleteEverySecond)
            {
                newValue = firstValue;
                if (m_maxChartValueY - 0.0001f >= secondValue)
                    searchForNewMaxValue = true;
            }
            if (m_smoothingType == SmoothingType.average)
            {
                newValue = (firstValue + secondValue) * 0.5f;
                if (firstValue >= m_maxChartValueY - 0.0001f || secondValue >= m_maxChartValueY - 0.0001f)
                    searchForNewMaxValue = true;
            }
            else if(m_smoothingType == SmoothingType.maximum)
            {
                newValue = Mathf.Max(firstValue, secondValue);
            }
            else if(m_smoothingType == SmoothingType.minimum)
            {
                newValue = Mathf.Min(firstValue, secondValue);
                if (firstValue >= m_maxChartValueY - 0.0001f || secondValue >= m_maxChartValueY - 0.0001f)
                    searchForNewMaxValue = true;
            }
            else if(m_smoothingType == SmoothingType.exact)
            {
                Debug.Log("Aborted: SmoothingType.exact is not implemented.");

                //int newBatchSize = (m_rawValues.Count - m_batchSize) / m_batchedValues.Count * 2;
                //m_batchSize = newBatchSize;

                //int startIndex = i * newBatchSize;
                //int endIndex = (i + 1) * newBatchSize - 1;

                //for(int j = startIndex; j <= endIndex; j++)
                //{
                //    newValue += m_rawValues[j];
                //}
                //newValue /= newBatchSize;
            }
            newBatchedValues.Add(newValue);
        }

        if (searchForNewMaxValue)
        {
            ChangeMaxValueY(GetHighestY(ref newBatchedValues));

        }
        for (int i = 0; i < m_maxNumberPoints / 2; i++)
        {
            m_batchedValues[i] = newBatchedValues[i];
            m_valueLine.SetPosition(i, new Vector3(m_valueLine.GetPosition(2 * i).x, ValueToPositionY(newBatchedValues[i]), 0));
        }
        m_valueLine.SetPosition(newBatchedValues.Count, m_valueLine.GetPosition(m_maxNumberPoints));
        for (int i = 0; i < m_maxNumberPoints / 2; i++)
        {
            m_batchedValues.RemoveAt(m_batchedValues.Count - 2);
        }
        m_valueLine.positionCount = (m_maxNumberPoints / 2) + 1;
        m_batchSize *= 2;
        m_currentGapPercent *= 2;
    }
    #endregion

    #region Misc
    private float GetNextX()
    {
        float x = m_currentPositionXPercent + m_currentGapPercent;
        m_currentPositionXPercent = x;
        return m_width * x;
    }
    #endregion
}
