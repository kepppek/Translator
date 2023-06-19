using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Library;  

namespace FormMain
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            Library.Resources.Report = Report;
            Library.Resources.Result = Result;
            Library.Resources.SourceCode = SourceCode;
            PrintCursorLocation();
        }

        /// <summary>
        /// Нажатие по кнопке "Перевести"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Parse_Click(object sender, EventArgs e)
        {
            Library.Parse.Translate();
        }

        /// <summary>
        /// Нажатие по кнопке "Открыть"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Open_Click(object sender, EventArgs e)
        {
            Library.FileSystem.Open();
        }

        /// <summary>
        /// Нажатие по кнопке "Сохранить"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Save_Click(object sender, EventArgs e)
        {
            Library.FileSystem.Save();
        }

        /// <summary>
        /// Нажатие по кнопке "Выполнить"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Run_Click(object sender, EventArgs e)
        {
            Library.FileSystem.Run();
        }

        /// <summary>
        /// Нажатие по кнопке "Скомпилировать"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Compile_Click(object sender, EventArgs e)
        {
            Library.FileSystem.Compile(true);
        }

        /// <summary>
        /// Событие нажатия мыши в sourcecode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SourceCode_MouseUp(object sender, MouseEventArgs e)
        {
            PrintCursorLocation();
        } 

        /// <summary>
        /// Событие нажатия клавиши в sourcecode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SourceCode_KeyUp(object sender, KeyEventArgs e)
        {
            PrintCursorLocation();
        }

        /// <summary>
        /// Вычисляет положение указателя
        /// </summary>
        void PrintCursorLocation()
        {
            int line = 1;
            int symbol = 1;

            int index = SourceCode.SelectionStart + SourceCode.SelectionLength;
            int count = 0;

            for (int i = 0; i < SourceCode.Lines.Length; i++)
            {
                count += SourceCode.Lines[i].Length+1;

                if(count>index)
                {
                    line = i+1;
                    symbol = index-(count- SourceCode.Lines[i].Length-2);
                    break;
                }
            }

            Line.Text = line.ToString();
            Symbol.Text = symbol.ToString();
        }
    }
}
