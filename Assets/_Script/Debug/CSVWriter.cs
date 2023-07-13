using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class CSVWriter : MonoBehaviour
{
    // khoi tao ten file
    private string filename = "";
    [System.Serializable]
    // class chua cac bien su dung de in ra
    public class Information
    {
        public double Time;
        public double Conlision;
        public double Complete;

    }
    // Start is called before the first frame update
    void Start()
    {
        // dat ten file la Output.csv
        filename = Application.dataPath + "Output.csv";
    }

    // Update is called once per frame
    void Update()
    {
        // Get thong tin moi lan nhan space
        if (Input.GetKeyDown(KeyCode.Space))
            WriteCSV();
    }

    public void WriteCSV()
    {
        // neu chua co file thi tao cac cot
        TextWriter tw = new StreamWriter(filename, false);
        tw.WriteLine("Time, Conlision, Complete");
        tw.Close();
        // lay thong tin 1000 lan theo tung du lieu time , conlision, complete
        for (int i = 0; i < 1000; i++)
        {
            tw.WriteLine(Time + "," + Conlision + "," + Complete);
        }
        tw.Close();
    }
}
