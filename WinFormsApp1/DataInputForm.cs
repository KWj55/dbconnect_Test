using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public class DataInputForm : Form
    {
        private Dictionary<string, Control> inputControls = new Dictionary<string, Control>();
        private Dictionary<string, string> columnTypes;
        private Button btnSubmit;
        private Button btnCancel;
        private string tableName;
        private DatabaseManager dbManager;

        public Dictionary<string, object> FormData { get; private set; }

        public DataInputForm(string tableName, Dictionary<string, string> columnTypes)
        {
            this.tableName = tableName;
            this.columnTypes = columnTypes;
            this.dbManager = DatabaseManager.Instance;

            InitializeComponent();
            CreateInputFields();
        }

        private void InitializeComponent()
        {
            this.Text = $"새 {tableName} 추가";
            this.Size = new Size(400, 300);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.AutoScroll = true;

            // 버튼 초기화
            btnSubmit = new Button
            {
                Text = "확인",
                DialogResult = DialogResult.OK,
                Size = new Size(75, 23),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };

            btnCancel = new Button
            {
                Text = "취소",
                DialogResult = DialogResult.Cancel,
                Size = new Size(75, 23),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };

            // 버튼 위치 설정
            btnCancel.Location = new Point(this.ClientSize.Width - 85, this.ClientSize.Height - 35);
            btnSubmit.Location = new Point(btnCancel.Left - 85, this.ClientSize.Height - 35);

            this.Controls.Add(btnSubmit);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnSubmit;
            this.CancelButton = btnCancel;

            btnSubmit.Click += BtnSubmit_Click;
        }

        private void CreateInputFields()
        {
            int yPos = 20;
            bool isFirstColumn = true;
            string idColumnName = $"{tableName.ToLower()}id";

            foreach (var column in columnTypes)
            {
                // 레이블 생성
                var label = new Label
                {
                    Text = column.Key + ":",
                    Location = new Point(20, yPos),
                    AutoSize = true
                };
                this.Controls.Add(label);

                Control inputControl;

                // 첫 번째 컬럼(ID)인 경우
                if (isFirstColumn)
                {
                    // ID 값 자동 생성 (해당 컬럼의 최대값 + 1 사용)
                    int maxId = dbManager.GetMaxId(tableName, column.Key);

                    var textBox = new TextBox
                    {
                        Location = new Point(150, yPos),
                        Size = new Size(200, 20),
                        ReadOnly = true,
                        Text = (maxId + 1).ToString(),
                        BackColor = SystemColors.Control  // 읽기 전용 표시를 위한 배경색 변경
                    };
                    inputControl = textBox;
                    isFirstColumn = false;
                }
                else
                {
                    // 데이터 타입에 따른 컨트롤 생성
                    switch (column.Value.ToLower())
                    {
                        case "datetime":
                            var dateTimePicker = new DateTimePicker
                            {
                                Location = new Point(150, yPos),
                                Size = new Size(200, 20),
                                Format = DateTimePickerFormat.Short
                            };
                            inputControl = dateTimePicker;
                            break;

                        case "int":
                            var numericUpDown = new NumericUpDown
                            {
                                Location = new Point(150, yPos),
                                Size = new Size(200, 20),
                                Maximum = 1000000,
                                Minimum = 0
                            };
                            inputControl = numericUpDown;
                            break;

                        default:
                            var textBox = new TextBox
                            {
                                Location = new Point(150, yPos),
                                Size = new Size(200, 20)
                            };
                            inputControl = textBox;
                            break;
                    }
                }

                this.Controls.Add(inputControl);
                inputControls.Add(column.Key, inputControl);
                yPos += 30;
            }

            // 폼 크기 조정
            this.ClientSize = new Size(400, yPos + 50);  // 입력 필드들 + 버튼을 위한 여유 공간
            
            // 버튼 위치 재조정
            btnCancel.Location = new Point(this.ClientSize.Width - 85, this.ClientSize.Height - 35);
            btnSubmit.Location = new Point(btnCancel.Left - 85, this.ClientSize.Height - 35);
        }

        private void BtnSubmit_Click(object sender, EventArgs e)
        {
            FormData = new Dictionary<string, object>();

            foreach (var control in inputControls)
            {
                object value = null;

                switch (control.Value)
                {
                    case TextBox textBox:
                        value = textBox.Text;
                        if (columnTypes[control.Key] == "int")
                        {
                            if (int.TryParse(textBox.Text, out int intValue))
                                value = intValue;
                            else
                            {
                                MessageBox.Show($"{control.Key}는 숫자여야 합니다.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                this.DialogResult = DialogResult.None;
                                return;
                            }
                        }
                        break;

                    case NumericUpDown numericUpDown:
                        value = (int)numericUpDown.Value;
                        break;

                    case DateTimePicker dateTimePicker:
                        value = dateTimePicker.Value;
                        break;
                }

                if (value == null || (value is string str && string.IsNullOrWhiteSpace(str)))
                {
                    MessageBox.Show($"{control.Key}를 입력해주세요.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.DialogResult = DialogResult.None;
                    return;
                }

                FormData.Add(control.Key, value);
            }
        }
    }
} 