using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using System.Data;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace Library
{
    /// <summary>
    /// Хранит ошибки анализатора и ссылки на textbox
    /// </summary>
    public static class Resources
    {
        public static TextBox Report;
        public static TextBox Result;
        public static RichTextBox SourceCode;

        public static string errors = "";

        public static void AddError(string text)
        {
            errors += text + "\r\n";
        }
    }

    /// <summary>
    /// Отвечает за корректную работу файловой системы открыть/сохранить
    /// </summary>
    public static class FileSystem
    {
        static string pathSourceCode = "";
        static string nameFile = "";
        static string pathFASM = "";

        /// <summary>
        /// Открывает выбранный файл и заполняет sourcecode
        /// </summary>
        public static void Open()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Text files|*.txt";
            string code = "";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                pathSourceCode = System.IO.Path.GetDirectoryName(ofd.FileName);
                nameFile = System.IO.Path.GetFileNameWithoutExtension(ofd.FileName);

                code = File.ReadAllText(ofd.FileName);
                Resources.SourceCode.Text = code;
            }
        }

        /// <summary>
        /// Сохраняет файл
        /// </summary>
        public static void Save()
        {
            if (File.Exists(pathSourceCode + "\\" + nameFile + ".txt"))
            {
                File.WriteAllText(pathSourceCode + "\\" + nameFile + ".txt", Resources.SourceCode.Text);
                MessageBox.Show("Файл сохранен!", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "Text files|*.txt";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    pathSourceCode = System.IO.Path.GetDirectoryName(sfd.FileName);
                    nameFile = System.IO.Path.GetFileNameWithoutExtension(sfd.FileName);

                    if (File.Exists(sfd.FileName))
                    {
                        File.WriteAllText(sfd.FileName, Resources.SourceCode.Text);
                    }
                    else
                    {
                        pathSourceCode += "\\" + nameFile;
                        Directory.CreateDirectory(pathSourceCode);

                        File.WriteAllText(pathSourceCode + "\\" + System.IO.Path.GetFileName(sfd.FileName), Resources.SourceCode.Text);
                    }
                    MessageBox.Show("Файл сохранен!", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        /// <summary>
        /// Запускает исполняемый файл
        /// </summary>
        public static void Run()
        {
            try
            {
                if (Compile(false) == false)
                    return;

                System.Diagnostics.Process p = new System.Diagnostics.Process();
                p.StartInfo.FileName = pathSourceCode + "\\" + nameFile + ".exe";
                p.Start();
            }
            catch (Exception E)
            {
                MessageBox.Show(E.Message, "Произошла ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Компилирует текст в exe файл
        /// </summary>
        /// <param name="flag"></param>
        /// <returns></returns>
        public static bool Compile(bool flag)
        {
            try
            {
                if (pathSourceCode == "")
                {
                    throw new Exception("Необходимо сохранить код в файл!");
                }

                //Парсим код в ассемблер
                if (Parse.Translate() == false)
                    throw new Exception("В коде имеются ошибки!");

                //Ассемблерный код в файл
                File.WriteAllText(pathSourceCode + "\\" + nameFile + ".asm", Resources.Result.Text, Encoding.GetEncoding(1251));

                //Если FASM отсутствует с исполняемым файлом то просим указать путь к нему
                if (pathFASM == "")
                    if (File.Exists(System.IO.Path.GetDirectoryName(Application.ExecutablePath) + "\\FASM\\FASM.EXE") == false)
                    {
                        MessageBox.Show("Исполняемый файл компилятора не найден!\r\nУкажите путь к файлу FASM.EXE", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        OpenFileDialog ofd = new OpenFileDialog();

                        if (ofd.ShowDialog() == DialogResult.OK)
                        {
                            pathFASM = System.IO.Path.GetDirectoryName(ofd.FileName);
                        }
                    }
                    else
                        pathFASM = System.IO.Path.GetDirectoryName(Application.ExecutablePath) + "\\FASM";


                //Запускаем компилятор
                System.Diagnostics.Process p = new System.Diagnostics.Process();
                p.StartInfo.Arguments = @"/k cd " + pathFASM + " & FASM.EXE " + pathSourceCode + "\\" + nameFile + ".asm & exit";
                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                p.StartInfo.FileName = "C:\\Windows\\system32\\cmd.exe";
                p.Start();

                p.WaitForExit();
                if (flag)
                    MessageBox.Show("Компиляция в исполняемый файл\r\nпрошла успешно!", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Information);

                return true;
            }
            catch (Exception E)
            {
                MessageBox.Show(E.Message, "Произошла ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }

    /// <summary>
    /// Запускает анализатор и выводит результат на интерфйес
    /// </summary>
    public static class Parse
    {
        /// <summary>
        /// Запускает парсер
        /// </summary>
        /// <returns>Ошибки: false - есть/ true - нет</returns>
        public static bool Translate()
        {
            SyntaxAnalyzer.Compile();

            if (Resources.errors != "")
            {
                Resources.Report.Text = Resources.errors;
                Resources.errors = "";
                Resources.Result.Clear();
                return false;
            }

            Resources.Report.Clear();
            Resources.Result.Text = CodeGenerator.JoinASM();
            return true;
        }
    }

    /// <summary>
    /// Таблица переменных
    /// </summary>
    public class NameTable
    {
        /// <summary>
        /// Список переменных
        /// </summary>
        public static List<Variable> listVariable = new List<Variable>();

        /// <summary>
        /// Типы переменных
        /// </summary>
        public enum VariableType
        {
            logical, integer, none
        }

        /// <summary>
        /// Переменная
        /// </summary>
        public class Variable
        {
            /// <summary>
            /// Имя переменной
            /// </summary>
            public string name;
            /// <summary>
            /// Тип переменной
            /// </summary>
            public VariableType type;

            public Variable(string Name, VariableType Type)
            {
                name = Name;
                type = Type;
            }
        }

        /// <summary>
        /// Добавляет переменную в список переменных
        /// </summary>
        /// <param name="name">Имя переменной</param>
        /// <param name="type">Тип переменной</param>
        /// <returns>true - успех/ false - неудача</returns>
        public static bool AddVariable(string name, VariableType type)
        {
            for (int i = 0; i < listVariable.Count; i++)
                if (listVariable[i].name == name)
                    return true;

            listVariable.Add(new Variable(name, type));

            return false;
        }

        /// <summary>
        /// Ищет совпадение в списке переменных
        /// </summary>
        /// <param name="name">Имя переменной</param>
        /// <returns>не none - присутствует /none - отсутствует</returns>
        public static NameTable.VariableType FindVariable(string name)
        {
            for (int i = 0; i < listVariable.Count; i++)
                if (listVariable[i].name == name)
                    return listVariable[i].type;

            return VariableType.none;
        }

        /// <summary>
        /// Очищает список переменных
        /// </summary>
        public static void Clear()
        {
            listVariable.Clear();
        }
    }

    /// <summary>
    /// Лексический анализатор
    /// </summary>
    public static class LexicalAnalyzer
    {
        /// <summary>
        /// Содержит список лексем
        /// </summary>
        public static List<Lexem> listLexems = new List<Lexem>();
        /// <summary>
        /// Текущая лексема
        /// </summary>
        public static string currentLexem;
        /// <summary>
        /// Номер строки в тексте
        /// </summary>
        public static int line;
        /// <summary>
        /// Номер символа в строке
        /// </summary>
        public static int symbol;
        /// <summary>
        /// Номер символа в текущей лексеме
        /// </summary>
        public static int currentSymbol;
        /// <summary>
        /// Номер линии для текущей лексемы
        /// </summary>
        public static int currentLine;
        /// <summary>
        /// Содержит разделить для текущей лексемы
        /// </summary>
        public static char separator;
        /// <summary>
        /// Текущая строка
        /// </summary>
        public static string wordLine;

        /// <summary>
        /// Типы лексем
        /// </summary>
        public enum TypeLexem
        {
            Begin, End,        //начало-конец блока кода    
            Print,             //оператор печати
            Expression,        //выражение
            Unknown,           //неизвестный тип лексемы
            None,              //значение отсутствует
            Case,              //конструкция switch
            Until,             //цикл 
            Assign,            //присвоение 
            ExitUntil,         //метка выхода из цикла
            ExitCase,          //метка выхода из условия
        }

        /// <summary>
        /// Лексема
        /// </summary>
        public class Lexem
        {
            /// <summary>
            /// Содержание лексемы
            /// </summary>
            public string value;
            /// <summary>
            /// Тип лексемы
            /// </summary>
            public TypeLexem lexem;

            public Lexem(Lexem Lexem)
            {
                value = Lexem.value;
                lexem = Lexem.lexem;
            }

            public Lexem(string Value, TypeLexem Lexem)
            {
                value = Value;
                lexem = Lexem;
            }

            public Lexem()
            {
                value = "";
                lexem = TypeLexem.None;
            }
        }

        /// <summary>
        /// Получает лексему
        /// </summary>
        /// <param name="separators">Список разделителей</param>
        public static void NextLexem(char[] separators)
        {
            //Буфер для лексемы
            StringBuilder value = new StringBuilder(wordLine.Length);
            //Флаг если разделитель соответствует
            bool flagSeparator = false;

            //Сохраняем положение начала лексемы
            currentSymbol = symbol;
            currentLine = line;

            //Цикл перебора элементов
            for (int i = symbol - 1; i < wordLine.Length; i++)
            {
                //Если дошли до конца строки то присваиваем соответствующий разделитель
                if (i == wordLine.Length - 1)
                    separator = '\n';

                //Если вначале идет пробел то пропускаем итерацию
                if (wordLine[i] == ' ' && value.Length == 0)
                {
                    currentSymbol++;
                    symbol++;
                    continue;
                }

                //Проверяем разделители
                for (int j = 0; j < separators.Length; j++)
                    if (wordLine[i] == separators[j])
                    {
                        flagSeparator = true;
                        separator = separators[j];
                        break;
                    }

                //Если разделитель соответствует, то выходим
                if (flagSeparator == false)
                    value.Append(wordLine[i]);
                else
                    break;
            }

            currentLexem = value.ToString();
            symbol += value.Length + 1;

            //Если достигли конца строки, то переходим на следующую строчку
            if (symbol - 1 >= wordLine.Length)
                NextLine();
        }

        /// <summary>
        /// Увеличивает номер символа лексемы на 'count'
        /// </summary>
        /// <param name="count">Количество добавляемых пунктов</param>
        public static void NextSymbol(int count)
        {
            currentSymbol += count;
        }

        /// <summary>
        /// Переходим на следующую строчку
        /// </summary>
        public static void NextLine()
        {
            line++;
            symbol = 1;

            if (Resources.SourceCode.Lines.Length > line - 1)
                wordLine = Resources.SourceCode.Lines[line - 1];
        }

        /// <summary>
        /// Добавляет лексему в список лексем
        /// </summary>
        /// <param name="Value">Содержание лексемы</param>
        /// <param name="Lexem">ТипЛексемы</param>
        public static void AddLexem(string Value, TypeLexem Lexem)
        {
            listLexems.Add(new Lexem() { value = Value, lexem = Lexem });
        }

        /// <summary>
        /// Обнуляет класс лексический
        /// </summary>
        public static void Clear()
        {
            line = 1;
            symbol = 1;
            listLexems.Clear();
            currentLexem = "";

            if (Resources.SourceCode.Text != "")
                wordLine = Resources.SourceCode.Lines[line - 1];
        }
    }

    /// <summary>
    /// Синтаксический анализатор
    /// </summary>
    public static class SyntaxAnalyzer
    {
        /// <summary>
        /// Тип лексемы
        /// </summary>
        static LexicalAnalyzer.TypeLexem type;
        /// <summary>
        /// Хранит строчку объявления цикла
        /// </summary>
        static Stack<int> positionUntil = new Stack<int>();
        /// <summary>
        /// Был/небыл 'begin'
        /// </summary>
        static bool flagBegin = false;
        /// <summary>
        /// Список порядковый номеров - переходов для Case
        /// </summary>
        public static List<List<int>> caseEnd = new List<List<int>>();
        /// <summary>
        /// Служит для разделения Case в CG
        /// </summary>
        public static bool flagFirst;
        /// <summary>
        /// Номе
        /// </summary>
        static int indexCase;
        /// <summary>
        /// Номер текущего Case выражения
        /// </summary>
        public static int numberCase;
        /// <summary>
        /// Служит для разделения Until в CG
        /// </summary>
        public static bool flagUntil = false;

        ///Счетчики циклов
        static int start = 0;
        static Stack<int> end = new Stack<int>();

        /// <summary>
        /// Очищает класс синтаксический
        /// </summary>
        static void Clear()
        {
            flagBegin = false;
            positionUntil.Clear();
            type = LexicalAnalyzer.TypeLexem.None;
            start = 0;
            end.Clear();
            numberCase = 0;
            indexCase = 0;
            flagFirst = false;
            caseEnd.Clear();
        }
        /// <summary>
        /// Переводит код высокого уровня в ассемблерные функции 
        /// </summary>
        /// <returns>Ассемблерный код</returns>
        public static void Compile()
        {
            ParseData();
            CodeGenerator.WriteData();
            ParseCode();
            CodeGenerator.WriteCode();
        }
        /// <summary>
        /// Парсит блок переменных
        /// </summary>
        static void ParseData()
        {
            bool flagError = false;
            //Обнуляем поля
            NameTable.Clear();
            LexicalAnalyzer.Clear();

            //Если есть хоть какой-то текст
            if (Resources.SourceCode.Text.Length != 0)
            {
                //Тип переменных;
                NameTable.VariableType type;

                //Перебираем все строки вплоть до "begin"
                while (true)
                {
                    //Читаем лексему
                    LexicalAnalyzer.NextLexem(new char[] { ' ', '\n' });

                    //Если строка не пустая
                    if (LexicalAnalyzer.currentLexem != "")
                    {
                        //Если дошли до "begin", то выходим из цикла
                        if (LexicalAnalyzer.currentLexem == "begin")
                        {
                            //После 'begin' что-то есть 
                            while (LexicalAnalyzer.currentLine == LexicalAnalyzer.line)
                            {
                                LexicalAnalyzer.NextLexem(new char[] { ' ', '\n' });

                                //Выводим ошибку о лишнем тексте
                                if (LexicalAnalyzer.currentLexem != "")
                                {
                                    Resources.AddError($"После ключевого слова 'begin', " +
                                        $"текст должен отсутствовать " +
                                        $"[Стр. {LexicalAnalyzer.currentLine} Симв. " +
                                        $"{LexicalAnalyzer.currentSymbol}]");
                                }
                            }

                            flagBegin = true;
                            break;
                        }

                        //Обнуляем тип переменных
                        type = NameTable.VariableType.none;

                        //Проверяем лексему на соответствие типу данных
                        switch (LexicalAnalyzer.currentLexem)
                        {
                            case "logical":
                                type = NameTable.VariableType.logical;
                                break;
                            case "integer":
                                type = NameTable.VariableType.integer;
                                break;
                        }

                        //Если тип переменных отсутствует то ошибка иначе парсим выражение
                        if (type != NameTable.VariableType.none)
                        {
                            //true - агументы есть / false - аргументов нету
                            bool flagAvailable = false;

                            List<int> commas = new List<int>();
                            int lastComma = -1;

                            //Перебираем все аргументы
                            while (LexicalAnalyzer.currentLine == LexicalAnalyzer.line)
                            {
                                LexicalAnalyzer.NextLexem(new char[] { ',', '\n' });

                                if (LexicalAnalyzer.currentLexem == "" && LexicalAnalyzer.separator == ',')
                                    commas.Add(LexicalAnalyzer.currentSymbol);
                                else
                                {
                                    //Проверяем на корректность
                                    CheckName(LexicalAnalyzer.currentLexem, type);

                                    //Если имя содержит что-то кроме пробелов
                                    if (LexicalAnalyzer.currentLexem.Trim().Length != 0)
                                    {
                                        flagAvailable = true;

                                        if (LexicalAnalyzer.separator == '\n')
                                            lastComma = -1;
                                        else
                                            lastComma = LexicalAnalyzer.currentSymbol;
                                    }
                                }
                            }

                            //Если запятая лишняя, то доабвим в список ошибок
                            if (lastComma != -1)
                                commas.Add(lastComma);

                            //Проверяем на лишние запятые
                            for (int i = 0; i < commas.Count; i++)
                            {
                                Resources.AddError($"Лишняя запятая " +
                                   $"[Стр. {LexicalAnalyzer.currentLine} Симв. {commas[i]}]");
                            }

                            //Проверяем на наличие аргументов
                            if (flagAvailable == false)
                            {
                                Resources.AddError($"Ожидался список переменных " +
                                    $"[Стр. {LexicalAnalyzer.currentLine} Симв. {LexicalAnalyzer.symbol}]");
                            }
                        }
                        else
                        {
                            Resources.AddError("Имя '" + LexicalAnalyzer.currentLexem
                            + $"' не существует в текущем контексте " +
                            $"[Стр. {LexicalAnalyzer.currentLine} Симв. {LexicalAnalyzer.currentSymbol}]");

                            //Если не перешли на след. строку, то переходим
                            if (LexicalAnalyzer.currentLine == LexicalAnalyzer.line)
                                LexicalAnalyzer.NextLine();
                        }
                    }

                    //Если дошли до конца кода и не нашли 'begin', то ошибка
                    if (LexicalAnalyzer.line - 1 >= Resources.SourceCode.Lines.Length)
                    {
                        flagError = true;
                        break;
                    }
                }
            }
            else
                flagError = true;

            if (flagError)
            {
                Resources.AddError($"Отсутствует ключевое слово 'begin' " +
                           $"[Стр. {LexicalAnalyzer.line} Симв. {LexicalAnalyzer.symbol}]");
            }
        }
        /// <summary>
        /// Парсит блок логики
        /// </summary>
        static void ParseCode()
        {
            //Если не нашли 'begin', то выходим
            if (flagBegin == false)
                return;

            //Обнуляем поля
            Clear();

            //Перебираем все строки вплоть до "end"
            while (true)
            {
                //Читаем лексему
                LexicalAnalyzer.NextLexem(new char[] { ' ', '=', '\n' });

                //Если строка не пустая
                if (LexicalAnalyzer.currentLexem != "")
                {
                    //Если дошли до "end", то выходим из цикла
                    if (LexicalAnalyzer.currentLexem == "end")
                    {
                        //После 'end' что-то есть 
                        while (LexicalAnalyzer.line - 1 < Resources.SourceCode.Lines.Length)
                        {
                            LexicalAnalyzer.NextLexem(new char[] { ' ', '\n' });

                            //Выводим ошибку о лишнем тексте
                            if (LexicalAnalyzer.currentLexem != "")
                            {
                                Resources.AddError($"После ключевого слова 'end', " +
                                    $"текст должен отсутствовать " +
                                    $"[Стр. {LexicalAnalyzer.currentLine} Симв. " +
                                    $"{LexicalAnalyzer.currentSymbol}]");
                            }
                        }

                        break;
                    }

                    //Обнуляем тип переменных
                    type = LexicalAnalyzer.TypeLexem.None;

                    //Проверяем лексему на соответствие ключевому слову
                    switch (LexicalAnalyzer.currentLexem)
                    {
                        case "print":
                            type = LexicalAnalyzer.TypeLexem.Print;
                            break;
                        case "case":
                            type = LexicalAnalyzer.TypeLexem.Case;
                            break;
                        case "until":
                            type = LexicalAnalyzer.TypeLexem.Until;
                            break;
                        case "enduntil":
                            positionUntil.Pop();

                            LexicalAnalyzer.listLexems.Add(new LexicalAnalyzer.Lexem(end.Pop().ToString(), LexicalAnalyzer.TypeLexem.ExitUntil));

                            type = LexicalAnalyzer.TypeLexem.ExitUntil;

                            //После 'enduntil' что-то есть 
                            while (LexicalAnalyzer.currentLine == LexicalAnalyzer.line)
                            {
                                LexicalAnalyzer.NextLexem(new char[] { ' ', '\n' });

                                //Выводим ошибку о лишнем тексте
                                if (LexicalAnalyzer.currentLexem != "")
                                {
                                    Resources.AddError($"После ключевого слова 'enduntil', " +
                                        $"текст должен отсутствовать " +
                                        $"[Стр. {LexicalAnalyzer.currentLine} Симв. " +
                                        $"{LexicalAnalyzer.currentSymbol}]");
                                }
                            }
                            break;
                    }

                    //Определяем принадлежность к переменной
                    if (type == LexicalAnalyzer.TypeLexem.None)
                    {
                        if (LexicalAnalyzer.separator == '=')
                            type = LexicalAnalyzer.TypeLexem.Assign;
                        else
                        {
                            //Если не нашли равно,то ищем
                            if (LexicalAnalyzer.currentLine == LexicalAnalyzer.line)
                            {
                                string buffer = LexicalAnalyzer.currentLexem;
                                int symbol = LexicalAnalyzer.currentSymbol;

                                LexicalAnalyzer.NextLexem(new char[] { '=', '\n' });

                                if (LexicalAnalyzer.separator == '=')
                                {
                                    type = LexicalAnalyzer.TypeLexem.Assign;
                                    LexicalAnalyzer.currentLexem = buffer;
                                    LexicalAnalyzer.currentSymbol = symbol;
                                }
                                else
                                    LexicalAnalyzer.currentLexem = buffer;
                            }
                        }
                    }
                    else
                        LexicalAnalyzer.currentLexem = "";

                    //Если ключевое слово не определенно, то ошибка иначе парсим выражение
                    if (type != LexicalAnalyzer.TypeLexem.None)
                    {
                        //Сравниваем типы
                        switch (type)
                        {
                            case LexicalAnalyzer.TypeLexem.Assign:
                                ParseExpression();
                                break;
                            case LexicalAnalyzer.TypeLexem.Case:
                                ParseCase();
                                break;
                            case LexicalAnalyzer.TypeLexem.Print:
                                ParsePrint();
                                break;
                            case LexicalAnalyzer.TypeLexem.Until:
                                ParseUntil();
                                break;
                        }
                    }
                    else
                    {
                        Resources.AddError("Имя '" + LexicalAnalyzer.currentLexem
                        + $"' не существует в текущем контексте " +
                        $"[Стр. {LexicalAnalyzer.currentLine} Симв. {LexicalAnalyzer.currentSymbol}]");

                        //Если не перешли на след. строку, то переходим
                        if (LexicalAnalyzer.currentLine == LexicalAnalyzer.line)
                            LexicalAnalyzer.NextLine();
                    }
                }

                //Если дошли до конца кода и не нашли 'end', то ошибка
                if (LexicalAnalyzer.line - 1 >= Resources.SourceCode.Lines.Length)
                {
                    Resources.AddError($"Отсутствует ключевое слово 'end' " +
                        $"[Стр. {LexicalAnalyzer.currentLine} Симв. 1]");
                    break;
                }
            }
            //Цикл не закрыт
            if (positionUntil.Count != 0)
            {
                int count = positionUntil.Count;
                for (int i = 0; i < count; i++)
                    Resources.AddError($"Незакрытый цикл, отсутствует 'enduntil' " +
                        $"[Стр. {positionUntil.Pop()} Симв. 1]");
            }
        }
        /// <summary>
        /// Парсит арифметическое выражение в постфиксную форму и добавялет в список лексем
        /// </summary>
        /// <param name="name">Имя переменной которой будет присвоено выражение 
        /// Искл: '_' - значит добавляем в стек</param>
        /// <param name="type">Тип переменной которой присваиваем выражение</param>
        static void ParseExpression()
        {
            ///Вычисляет приоритет операции
            int PriorityOperation(char operation)
            {
                int level = -1;
                switch (operation)
                {
                    case '(':
                        level = 3;
                        break;
                    case ')':
                        level = 3;
                        break;
                    case '*':
                        level = 2;
                        break;
                    case '/':
                        level = 2;
                        break;
                    case '+':
                        level = 1;
                        break;
                    case '-':
                        level = 1;
                        break;
                    case '!':
                        level = 3;
                        break;
                    case '%':
                        level = 3;
                        break;
                    case '&':
                        level = 2;
                        break;
                    case '|':
                        level = 1;
                        break;
                    case '^':
                        level = 1;
                        break;
                }
                return level;
            }

            #region [переменные]
            //Слово которому присваиваем выражение
            string wordAssign = "";
            //true - если выражение пустое
            bool flagEmpty = false;
            //Тип левого операнда
            NameTable.VariableType typeAssign = NameTable.VariableType.none;
            #endregion

            if (LexicalAnalyzer.currentLine == LexicalAnalyzer.line)
            {
                #region [определение типа выражения]
                if (type == LexicalAnalyzer.TypeLexem.Assign)
                {
                    if (CheckName(LexicalAnalyzer.currentLexem, NameTable.VariableType.none))
                    {
                        LexicalAnalyzer.NextLine();
                        return;
                    }

                    typeAssign = NameTable.FindVariable(LexicalAnalyzer.currentLexem);
                    wordAssign = LexicalAnalyzer.currentLexem;
                }
                else if (type == LexicalAnalyzer.TypeLexem.Until)
                {
                    typeAssign = NameTable.VariableType.logical;
                }
                else if (type == LexicalAnalyzer.TypeLexem.Case)
                {
                    typeAssign = NameTable.VariableType.integer;
                }
                #endregion

                #region [переменные]
                //Хранит операции и скобки
                Stack<char> operations = new Stack<char>();

                //Хранит позицию открытой скобки в строке
                Stack<int> brackets = new Stack<int>();

                //Строка в постфиксной записи
                StringBuilder result = new StringBuilder(LexicalAnalyzer.wordLine.Length);

                //Разделители
                char[] separators;

                //Предпоследняя операция
                bool flagValue = false;

                //of / do
                bool flagEndWod = false;

                bool flagOperation = false;

                //Определяем список разделителей в зависимости от типа левого операнда
                if (typeAssign == NameTable.VariableType.logical)
                    separators = new char[] { '\n', '!', '&', '|', '^', '(', ')' };
                else
                    separators = new char[] { '\n', '-', '+', '*', '/', '(', ')' };

                #endregion

                LexicalAnalyzer.NextLexem(separators);

                //Проверка на наличие выражения 
                if (LexicalAnalyzer.currentLexem != "" || LexicalAnalyzer.separator != '\n')
                {
                    while (true)
                    {
                        //Если встретили операцию и есть значение, то добавляем его в результат
                        if (LexicalAnalyzer.currentLexem != "")
                        {
                            if (type == LexicalAnalyzer.TypeLexem.Until)
                            {
                                if (LexicalAnalyzer.currentLexem.Trim(' ').EndsWith(" do"))
                                {
                                    flagEndWod = true;
                                    LexicalAnalyzer.currentLexem = LexicalAnalyzer.currentLexem.Replace(" do", "");
                                }
                            }
                            else if (type == LexicalAnalyzer.TypeLexem.Case)
                            {
                                if (LexicalAnalyzer.currentLexem.Trim(' ').EndsWith(" of"))
                                {
                                    flagEndWod = true;
                                    LexicalAnalyzer.currentLexem = LexicalAnalyzer.currentLexem.Replace(" of", "");
                                }
                            }

                            //Проверяем на ошибки значение/имя
                            CheckValue(LexicalAnalyzer.currentLexem, typeAssign);

                            //Добавляем это значение даже если оно с ошибкой
                            result.Append(LexicalAnalyzer.currentLexem.Trim(' ') + " ");

                            flagValue = true;
                        }
                        else//Значение пустое, попался разделитель
                        {
                            /* if(flagValue==false)
                             {
                                 Resources.AddError($"Ожидалось значение " +
                                        $"[Стр. {LexicalAnalyzer.currentLine} Симв. {LexicalAnalyzer.currentSymbol}]");
                             }*/

                            //Если минус то проверяем на отрицательное число
                            if (LexicalAnalyzer.separator == '-' && flagValue == false)
                            {
                                //Считываем
                                LexicalAnalyzer.NextLexem(separators);

                                //Если отрицание скобки
                                if (LexicalAnalyzer.separator == '(')
                                {
                                    operations.Push('%');
                                }
                                //Если отрицание числа/переменной
                                else
                                {
                                    //Проверка на корректность
                                    CheckValue(LexicalAnalyzer.currentLexem, typeAssign);

                                    if (NameTable.FindVariable(LexicalAnalyzer.currentLexem) != NameTable.VariableType.none)
                                        //Переменная 
                                        result.Append(LexicalAnalyzer.currentLexem + " % ");
                                    else
                                        //Число 
                                        result.Append('-' + LexicalAnalyzer.currentLexem + " ");

                                    flagValue = true;
                                }
                            }
                        }

                        if (LexicalAnalyzer.separator != '\n')
                            if (LexicalAnalyzer.separator == ')')
                            {
                                int sizeStack = operations.Count + 1;

                                //Выталкиваем из стека вплоть до открывающейся скобки
                                for (int i = 0; i < sizeStack; i++)
                                {
                                    if (operations.Count == 0)
                                    {
                                        Resources.AddError($"Незакрытая скобка " +
                                           $"[Стр. {LexicalAnalyzer.currentLine} Симв. {LexicalAnalyzer.currentSymbol}]");
                                        break;
                                    }
                                    else if (operations.Peek() == '(')
                                    {
                                        operations.Pop();
                                        brackets.Pop();
                                        break;
                                    }
                                    else
                                        result.Append(operations.Pop() + " ");
                                }
                            }
                            else if (LexicalAnalyzer.separator == '(')
                            {
                                operations.Push('(');
                                brackets.Push(LexicalAnalyzer.currentSymbol);
                            }
                            else
                            {
                                if (flagValue == false)
                                {
                                    if ((LexicalAnalyzer.separator != '!' && typeAssign == NameTable.VariableType.logical) || typeAssign == NameTable.VariableType.integer)
                                    {
                                        Resources.AddError($"Ожидалось значение " +
                                         $"[Стр. {LexicalAnalyzer.currentLine} Симв. {LexicalAnalyzer.currentSymbol}]");
                                    }
                                }
                                else
                                    flagValue = false;


                                if (operations.Count == 0 || operations.Peek() == '(' || (LexicalAnalyzer.separator == '!' && operations.Peek() == '!'))
                                {
                                    operations.Push(LexicalAnalyzer.separator);
                                }
                                else if (PriorityOperation(LexicalAnalyzer.separator) > PriorityOperation(operations.Peek()))
                                {
                                    operations.Push(LexicalAnalyzer.separator);
                                }
                                else if (PriorityOperation(LexicalAnalyzer.separator) <= PriorityOperation(operations.Peek()))
                                {
                                    //Размер стека операций
                                    int sizeStack = operations.Count;
                                    //Приоритет новой операции
                                    int separatorPriority = PriorityOperation(LexicalAnalyzer.separator);

                                    //Выталкиваем из стека вплоть до открывающейся скобки
                                    for (int i = 0; i < sizeStack; i++)
                                    {
                                        if (separatorPriority > PriorityOperation(operations.Peek()) || operations.Peek() == '(')
                                        {
                                            operations.Push(LexicalAnalyzer.separator);
                                            break;
                                        }
                                        else
                                        {
                                            result.Append(operations.Pop() + " ");

                                            //Если дошли до конца стека, то добавим новую операцию в стек
                                            if (i == sizeStack - 1)
                                            {
                                                operations.Push(LexicalAnalyzer.separator);
                                            }
                                        }
                                    }
                                }

                            }


                        //Если дошли до конца строки, то выходим из цикла
                        if (LexicalAnalyzer.currentLine != LexicalAnalyzer.line)
                        {
                            if (flagValue == false)
                            {
                                Resources.AddError($"Ожидалось значение " +
                                 $"[Стр. {LexicalAnalyzer.currentLine} Симв. {LexicalAnalyzer.currentSymbol}]");
                            }

                            if (type == LexicalAnalyzer.TypeLexem.Until)
                            {
                                if (flagEndWod == false)
                                {
                                    Resources.AddError($"Отсутствует ключевое слово ' do' " +
                                    $"[Стр. {LexicalAnalyzer.currentLine} Симв. {LexicalAnalyzer.currentSymbol}]");
                                }
                            }
                            else if (type == LexicalAnalyzer.TypeLexem.Case)
                            {
                                if (flagEndWod == false)
                                {
                                    Resources.AddError($"Отсутствует ключевое слово ' of' " +
                                      $"[Стр. {LexicalAnalyzer.currentLine} Симв. {LexicalAnalyzer.currentSymbol}]");
                                }
                            }

                            //Количество знаков
                            int sizeStack = operations.Count;

                            //Если что-то осталось перекладываем в выражение
                            for (int i = 0; i < sizeStack; i++)
                            {
                                if (operations.Peek() == '(')
                                {
                                    operations.Pop();
                                    Resources.AddError($"Незакрытая скобка " +
                                     $"[Стр. {LexicalAnalyzer.currentLine} Симв. {brackets.Pop()}]");
                                }
                                else if (operations.Peek() != '\n')
                                    result.Append(operations.Pop() + " ");
                                else
                                    operations.Pop();
                            }

                            string word = result.ToString().Trim(' ');

                            if (word.Length != 0)
                            {
                                LexicalAnalyzer.listLexems.Add(new LexicalAnalyzer.Lexem(word, LexicalAnalyzer.TypeLexem.Expression));

                                if (type == LexicalAnalyzer.TypeLexem.Assign)
                                    LexicalAnalyzer.listLexems.Add(new LexicalAnalyzer.Lexem(wordAssign, LexicalAnalyzer.TypeLexem.Assign));
                            }
                            else
                                flagEmpty = true;
                            break;
                        }
                        LexicalAnalyzer.NextLexem(separators);


                        // flagValue = false;
                    }
                }
                else
                    flagEmpty = true;
            }
            else
                flagEmpty = true;

            if (flagEmpty)
                Resources.AddError($"Ожидалось выражение " +
                    $"[Стр. {LexicalAnalyzer.currentLine} Симв. {LexicalAnalyzer.currentSymbol}]");
        }
        /// <summary>
        /// Парсит print
        /// </summary>
        static void ParsePrint()
        {
            bool flagEmpty = false;

            if (LexicalAnalyzer.separator != '\n')
            {
                LexicalAnalyzer.NextLexem(new char[] { '\n' });

                if (LexicalAnalyzer.currentLexem != "")
                {
                    //Удаляем пробелы справа
                    string word = LexicalAnalyzer.currentLexem.Trim(' ');

                    if (CheckName(word, NameTable.VariableType.none) == false)
                        LexicalAnalyzer.listLexems.Add(new LexicalAnalyzer.Lexem("[" + word + "]", LexicalAnalyzer.TypeLexem.Print));
                }
                else
                    flagEmpty = true;
            }
            else
                flagEmpty = true;

            if (flagEmpty)
                Resources.AddError($"Ожидалось имя переменной " +
                          $"[Стр. {LexicalAnalyzer.currentLine} Симв. {LexicalAnalyzer.currentSymbol}]");
        }
        /// <summary>
        /// Парсит конструкцию case
        /// </summary>
        static void ParseCase()
        {
            //Флаг если выражение пустое
            bool flagEmpty = false;
            //Количество кейсов
            int count = 0;

            caseEnd.Add(new List<int>());

            if (LexicalAnalyzer.separator != '\n')
            {
                ParseExpression();

                while (true)
                {
                    bool flagError = false;

                    LexicalAnalyzer.NextLexem(new char[] { '"', '\n' });

                    if (LexicalAnalyzer.currentLexem.Trim(' ') == "endcase")
                        break;

                    if (LexicalAnalyzer.separator != '\n')
                    {
                        LexicalAnalyzer.NextLexem(new char[] { '"', '\n' });

                        if (LexicalAnalyzer.separator != '\n')
                        {
                            if (CheckValue(LexicalAnalyzer.currentLexem, NameTable.VariableType.none) == false)
                            {
                                string value = LexicalAnalyzer.currentLexem.Trim();

                                LexicalAnalyzer.NextLexem(new char[] { ':', '\n' });

                                if (LexicalAnalyzer.separator != '\n')
                                {
                                    LexicalAnalyzer.NextLexem(new char[] { '=', '\n' });

                                    if (LexicalAnalyzer.separator != '\n')
                                    {
                                        type = LexicalAnalyzer.TypeLexem.Assign;

                                        caseEnd[caseEnd.Count - 1].Add(indexCase);
                                        indexCase++;
                                        LexicalAnalyzer.listLexems.Add(new LexicalAnalyzer.Lexem(value, LexicalAnalyzer.TypeLexem.Case));

                                        ParseExpression();

                                        LexicalAnalyzer.listLexems.Add(new LexicalAnalyzer.Lexem(value, LexicalAnalyzer.TypeLexem.ExitCase));
                                    }
                                    else
                                        flagError = true;
                                }
                                else
                                    flagError = true;
                            }
                        }
                        else
                            flagError = true;
                    }
                    else
                        if (LexicalAnalyzer.currentLexem != "")
                        flagError = true;

                    if (flagError)
                    {
                        Resources.AddError($"Case выражение некорректно " +
                            $"[Стр. {LexicalAnalyzer.currentLine} Симв. {LexicalAnalyzer.currentSymbol}]");
                    }

                    if (LexicalAnalyzer.line - 1 >= Resources.SourceCode.Lines.Length)
                    {
                        Resources.AddError($"Отсутствует ключевое слово 'endcase' " +
                            $"[Стр. {LexicalAnalyzer.currentLine} Симв. {LexicalAnalyzer.currentSymbol}]");
                        break;
                    }
                }
            }
            else
                flagEmpty = true;

            if (flagEmpty)
                Resources.AddError($"Ожидалась конструкция case <выражение> of " +
                          $"[Стр. {LexicalAnalyzer.currentLine} Симв. {LexicalAnalyzer.currentSymbol + LexicalAnalyzer.currentLexem.Length}]");
        }
        /// <summary>
        /// Парсит цикл until
        /// </summary>
        static void ParseUntil()
        {
            bool flagEmpty = false;

            if (LexicalAnalyzer.separator != '\n')
            {
                positionUntil.Push(LexicalAnalyzer.currentLine);

                LexicalAnalyzer.listLexems.Add(new LexicalAnalyzer.Lexem
                    (start.ToString(), LexicalAnalyzer.TypeLexem.Until));

                ParseExpression();

                end.Push(start);


                LexicalAnalyzer.listLexems.Add(new LexicalAnalyzer.Lexem
                    (start.ToString(), LexicalAnalyzer.TypeLexem.Until));

                start++;
            }
            else
                flagEmpty = true;

            if (flagEmpty)
                Resources.AddError($"Ожидалась конструкция until <выражение> do " +
                          $"[Стр. {LexicalAnalyzer.currentLine} Симв. " +
                          $"{LexicalAnalyzer.currentSymbol}]");
        }
        /// <summary>
        /// Проверяет переменную или значение на корректность
        /// </summary>
        /// <param name="name">Значение</param>
        /// <param name="type">Тип которому должно соответствовать значение</param>
        /// <returns>Значение: true - некорректно/ fasle - корректно</returns>
        static bool CheckValue(string name, NameTable.VariableType type)
        {
            //Флаг ошибки true - есть / false - нет 
            bool flagError = false;
            //Флаг символ был
            bool flagSymbol = false;
            //Количество цифр в переменной
            int countDigit = 0;

            //Убираем пробелы по сторонам
            name = name.Trim(' ');

            if (name != "")
            {
                for (int i = 0; i < name.Length; i++)
                {
                    if (name[i] == ' ' && flagSymbol)
                    {
                        Resources.AddError($"Значение некорректно " +
                               $"[Стр. {LexicalAnalyzer.currentLine} Симв. " +
                               $"{LexicalAnalyzer.currentSymbol}]");
                        flagError = true;
                        break;
                    }

                    if (name[i] != ' ')
                        flagSymbol = true;


                    //Если это число
                    if (Char.IsDigit(name[i]))
                        countDigit++;
                }

                if (flagError == false)
                {
                    //Если это число
                    if (countDigit == name.Length)
                    {
                        int value;

                        if (type == NameTable.VariableType.integer)
                        {
                            if (int.TryParse(name, out value) == false)
                            {
                                Resources.AddError($"Число превышает размер целочисленного типа " +
                                    $"[Стр. {LexicalAnalyzer.currentLine} Симв. {LexicalAnalyzer.currentSymbol}]");
                                flagError = true;
                            }
                            if (name[0] == '0' && name.Length > 1)
                            {
                                Resources.AddError($"Значение не может начинаться с нуля " +
                                      $"[Стр. {LexicalAnalyzer.currentLine} Симв. {LexicalAnalyzer.currentSymbol}]");
                                flagError = true;
                            }
                        }
                        else if (type == NameTable.VariableType.logical)
                        {
                            if (int.TryParse(name, out value))
                            {
                                if (name.Length > 1 || value > 1)
                                {
                                    Resources.AddError($"Несоответствие типов данных" +
                                      $"[Стр. {LexicalAnalyzer.currentLine} Симв. {LexicalAnalyzer.currentSymbol}]");
                                    flagError = true;
                                }
                            }
                        }
                    }
                    else if (countDigit == 0)
                    {
                        //Если в имени имеются ошибки, возвращаем ошибку
                        if (CheckName(name, NameTable.VariableType.none) == false)
                        {
                            if (NameTable.FindVariable(name) != type)
                            {
                                Resources.AddError($"Несоответствие типов данных" +
                                $"[Стр. {LexicalAnalyzer.currentLine} Симв. " +
                                $"{LexicalAnalyzer.currentSymbol}]");
                                flagError = true;
                            }
                        }
                        else
                            flagError = true;
                    }
                    else
                    {
                        Resources.AddError($"Значение некорректно " +
                          $"[Стр. {LexicalAnalyzer.currentLine} Симв. " +
                           $"{LexicalAnalyzer.currentSymbol}]");
                        flagError = true;
                    }
                }
            }
            else
            {

                Resources.AddError($"Ожидалось значение " +
                  $"[Стр. {LexicalAnalyzer.currentLine} Симв. {LexicalAnalyzer.currentSymbol}]");

                flagError = true;
            }

            return flagError;
        }
        /// <summary>
        /// Проверяет имя переменной на корректность</summary>
        /// <param name="name">Имя переменной</param>
        /// <param name="type">Тип добавляемой переменной / none - просто ищем, без добавления</param>
        /// <returns>Название: false - корректно
        /// /true - содержит ошибки</returns>
        static bool CheckName(string name, NameTable.VariableType type)
        {
            //Готовое слово
            StringBuilder word = new StringBuilder(name.Length);
            //Код символа
            int ascii;
            //Начальная позиция слова в строке
            int positionWord = 0;
            //true - между символами был пробел /false - не был
            bool flagSpace = false;
            //true - слово некорректно /false - слово корректно
            bool flagError = false;

            string[] keyWords = new[] { "begin", "end", "case", "until", "do", "of", "integer", "logical", "print" };

            for (int i = 0; i < name.Length; i++)
            {
                ascii = (int)name[i];

                //Если не пробел
                if (name[i] != ' ')
                {
                    //Если пробел между словами, то ошибка
                    if (flagSpace && type != NameTable.VariableType.none)
                    {
                        Resources.AddError($"Ожидался разделитель ',' " +
                            $"[Стр. {LexicalAnalyzer.currentLine} Симв. {LexicalAnalyzer.currentSymbol}]");
                        flagSpace = false;
                        flagError = true;
                        word.Clear();
                    }

                    //Если буква то добавляем
                    if ((ascii > 64 && ascii < 91) || (ascii > 96 && ascii < 123))
                    {
                        word.Append(name[i]);
                    }
                    /* else if (ascii > 47 && ascii < 58) //Если цифра
                     {
                         if (word.Length == 0)
                         {
                             Resources.AddError($"Имя переменной не может начинаться с цифры " +
                                 $"[Стр. {LexicalAnalyzer.currentLine} Симв. {LexicalAnalyzer.currentSymbol}]");
                             flagError = true;
                         }
                         else
                             word.Append(name[i]);
                     }//Если другой символ*/
                    else
                    {
                        Resources.AddError($"Имя переменной содержит запрещенные символы " +
                            $"[Стр. {LexicalAnalyzer.currentLine} Симв. {LexicalAnalyzer.currentSymbol}]");
                        flagError = true;
                    }

                    //Сохраняем позицию слова
                    if (word.Length == 1)
                        positionWord = LexicalAnalyzer.currentSymbol;
                }
                else //Если пробел между словами, то ставим флаг
                {
                    if (word.Length != 0)
                    {
                        flagSpace = true;
                    }
                }

                LexicalAnalyzer.currentSymbol++;
            }

            if (flagError == false)
                if (word.Length != 0)
                {
                    for (int i = 0; i < keyWords.Length; i++)
                    {
                        if (word.ToString() == keyWords[i])
                        {
                            Resources.AddError($"Имя переменной не может быть ключевы словом " +
                                 $"[Стр. {LexicalAnalyzer.currentLine} Симв. {positionWord}]");
                            flagError = true;
                            break;
                        }
                    }

                    if (flagError == false)
                    {
                        if (word.Length > 255)
                        {
                            Resources.AddError($"Имя переменной не должно превышать 255 символов " +
                                $"[Стр. {LexicalAnalyzer.currentLine} Симв. {positionWord}]");
                            flagError = true;
                        }
                        else
                        {
                            if (type != NameTable.VariableType.none)
                            {
                                if (NameTable.AddVariable(word.ToString(), type))
                                {
                                    Resources.AddError($"Переменная с таким именем уже определена " +
                                        $"[Стр. {LexicalAnalyzer.currentLine} Симв. {positionWord}]");
                                    flagError = true;
                                }
                            }
                            else
                            {
                                if (NameTable.FindVariable(word.ToString()) == NameTable.VariableType.none)
                                {
                                    Resources.AddError($"Переменная с таким именем неопределена " +
                                        $"[Стр. {LexicalAnalyzer.currentLine} Симв. {positionWord}]");
                                    flagError = true;
                                }
                            }
                        }
                    }
                }

            return flagError;
        }
    }

    /// <summary>
    /// Служит для разбора списка лексем и преобразования их в код asm
    /// </summary>
    public static class CodeGenerator
    {
        /// <summary>
        /// Содержит необъявленные переменные
        /// </summary>
        public static List<string> data = new List<string>();

        /// <summary>
        /// Содержит инструкции кода
        /// </summary>
        public static List<string> code = new List<string>();

        /// <summary>
        /// Содержит шаблон asm кода
        /// </summary>
        public static List<string> asm = new List<string>()
        {
            "format PE console",
            "entry address0",
            "include 'INCLUDE/win32a.inc'",
            "section '.data' data readable writable",
            "      db '%d',10,0",
            "section '.code' code readable executable",
            "      address0:",
            "      call [getch]",
            "section '.idata' import data readable",
            "   library msvcrt,'msvcrt.dll'",
            "   import msvcrt,\\",
            "      printf,'printf',\\",
            "      getch,'_getch'"
        };

        /// <summary>
        /// Добавляет инструкцию в список кода
        /// </summary>
        /// <param name="instruction"></param>
        public static void AddInstruction(string instruction)
        {
            code.Add("      " + instruction);
        }

        /// <summary>
        /// Добавляет переменную в список данных
        /// </summary>
        /// <param name="variable"></param>
        public static void AddData(string variable)
        {
            data.Add("      " + variable);
        }

        /// <summary>
        /// Добавляет список переменных asm в код
        /// </summary>
        public static void WriteCode()
        {
            //Если ошибок нет
            if (Resources.errors == "")
            {
                code.Clear();

                List<LexicalAnalyzer.Lexem> listLexems = LexicalAnalyzer.listLexems;

                for (int i = 0; i < listLexems.Count; i++)
                {
                    if (listLexems[i].lexem == LexicalAnalyzer.TypeLexem.Print)
                    {
                        AddInstruction("invoke printf,401000h," + listLexems[i].value);
                    }
                    else if (listLexems[i].lexem == LexicalAnalyzer.TypeLexem.Expression)
                    {
                        string[] operations = listLexems[i].value.Split(' ');

                        for (int j = 0; j < operations.Length; j++)
                        {
                            switch (operations[j])
                            {
                                case "+":
                                    AddInstruction(";   +   ");
                                    AddInstruction("pop ebx");
                                    AddInstruction("pop eax");
                                    AddInstruction("add eax,ebx");
                                    AddInstruction("push eax");
                                    break;
                                case "-":
                                    AddInstruction(";   -   ");
                                    AddInstruction("pop ebx");
                                    AddInstruction("pop eax");
                                    AddInstruction("sub eax,ebx");
                                    AddInstruction("push eax");
                                    break;
                                case "/":
                                    AddInstruction(";   /   ");
                                    AddInstruction("pop ebx");
                                    AddInstruction("pop eax");
                                    AddInstruction("cdq");
                                    AddInstruction("idiv ebx");
                                    AddInstruction("push eax");
                                    break;
                                case "*":
                                    AddInstruction(";   *   ");
                                    AddInstruction("pop eax");
                                    AddInstruction("pop ebx");
                                    AddInstruction("imul eax,ebx");
                                    AddInstruction("push eax");
                                    break;
                                case "^":
                                    AddInstruction(";   ^   ");
                                    AddInstruction("pop ebx");
                                    AddInstruction("pop eax");
                                    AddInstruction("xor eax,ebx");
                                    AddInstruction("push eax");
                                    break;
                                case "%"://арифметическое отрицание
                                    AddInstruction(";   -   ");
                                    AddInstruction("pop eax");
                                    AddInstruction("neg eax");
                                    AddInstruction("push eax");
                                    break;
                                case "!"://логическое отрицание
                                    AddInstruction(";   !   ");
                                    AddInstruction("pop eax");
                                    AddInstruction("btc eax,0");
                                    AddInstruction("push eax");
                                    break;
                                case "&":
                                    AddInstruction(";   &   ");
                                    AddInstruction("pop ebx");
                                    AddInstruction("pop eax");
                                    AddInstruction("and eax,ebx");
                                    AddInstruction("push eax");
                                    break;
                                case "|":
                                    AddInstruction(";   |   ");
                                    AddInstruction("pop ebx");
                                    AddInstruction("pop eax");
                                    AddInstruction("or eax,ebx");
                                    AddInstruction("push eax");
                                    break;
                                default:

                                    if (NameTable.FindVariable(operations[j]) == NameTable.VariableType.none)
                                        AddInstruction("push dword " + operations[j]);
                                    else
                                        AddInstruction("push [" + operations[j] + "]");
                                    break;
                            }
                        }
                    }
                    else if (listLexems[i].lexem == LexicalAnalyzer.TypeLexem.Assign)
                    {
                        AddInstruction(";   " + listLexems[i].value + "   ");
                        AddInstruction("pop [" + listLexems[i].value + "]");
                    }
                    else if (listLexems[i].lexem == LexicalAnalyzer.TypeLexem.Until)
                    {
                        if (SyntaxAnalyzer.flagUntil == false)
                        {
                            AddInstruction(";   until   ");
                            AddInstruction("startuntil" + listLexems[i].value + ":");
                            SyntaxAnalyzer.flagUntil = true;
                        }
                        else
                        {
                            AddInstruction("pop eax");
                            AddInstruction("cmp eax,dword 0");
                            AddInstruction("push eax");
                            AddInstruction("je enduntil" + listLexems[i].value);
                            SyntaxAnalyzer.flagUntil = false;
                        }
                    }
                    else if (listLexems[i].lexem == LexicalAnalyzer.TypeLexem.Case)
                    {
                        AddInstruction(";   case   ");
                        if (SyntaxAnalyzer.flagFirst == false)
                        {
                            AddInstruction("pop eax");
                            SyntaxAnalyzer.flagFirst = true;
                        }
                        AddInstruction("cmp eax," + listLexems[i].value);
                        AddInstruction("jne endcase" + SyntaxAnalyzer.caseEnd[SyntaxAnalyzer.numberCase][0]);
                    }
                    else if (listLexems[i].lexem == LexicalAnalyzer.TypeLexem.ExitUntil)
                    {
                        AddInstruction(";   enduntil   ");
                        AddInstruction("jmp startuntil" + listLexems[i].value);
                        AddInstruction("enduntil" + listLexems[i].value + ":");
                    }
                    else if (listLexems[i].lexem == LexicalAnalyzer.TypeLexem.ExitCase)
                    {
                        if (SyntaxAnalyzer.caseEnd[SyntaxAnalyzer.numberCase].Count != 1)
                        {
                            AddInstruction("jmp endcase" + SyntaxAnalyzer.caseEnd[SyntaxAnalyzer.numberCase]
                            [SyntaxAnalyzer.caseEnd[SyntaxAnalyzer.numberCase].Count - 1]);
                        }

                        AddInstruction("endcase" + SyntaxAnalyzer.caseEnd[SyntaxAnalyzer.numberCase][0] + ":");

                        SyntaxAnalyzer.caseEnd[SyntaxAnalyzer.numberCase].RemoveAt(0);

                        if (SyntaxAnalyzer.caseEnd[SyntaxAnalyzer.numberCase].Count == 0)
                        {
                            SyntaxAnalyzer.flagFirst = false;
                            SyntaxAnalyzer.numberCase++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Добавляет список переменных в asm код
        /// </summary>
        public static void WriteData()
        {
            //Если ошибок нет
            if (Resources.errors == "")
            {
                data.Clear();

                for (int i = 0; i < NameTable.listVariable.Count; i++)
                    AddData(NameTable.listVariable[i].name + " dd ?"); //выделяем 4 байта
            }
        }

        /// <summary>
        /// Соединяет все списки в asm список
        /// </summary>
        /// <returns></returns>
        public static string JoinASM()
        {
            //Если ошибок нет
            if (Resources.errors == "")
            {
                List<string> result = new List<string>(asm);
                if (code.Count != 0)
                    result.Insert(7, String.Join("\r\n", code));
                if (data.Count != 0)
                    result.Insert(5, String.Join("\r\n", data));

                return String.Join("\r\n", result);
            }
            else
                return "";
        }
    }
}
