using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public class DataEditForm : Form
    {
        private Dictionary<string, Control> inputControls = new Dictionary<string, Control>();
        private Dictionary<string, string> columnTypes;
        private Button btnSubmit;
        private Button btnCancel;
        private string tableName;
        private string idColumnName;
        private Dictionary<string, object> existingData;

        public Dictionary<string, object> FormData { get; private set; }

        public DataEditForm(string tableName, Dictionary<string, string> columnTypes, Dictionary<string, object> existingData, string idColumnName)
        {
            this.tableName = tableName;
            this.columnTypes = columnTypes;
            this.existingData = existingData;
            this.idColumnName = idColumnName;

            InitializeComponent();
            CreateInputFields();
        }

        private void InitializeComponent()
        {
            this.Text = $"{tableName} 수정";
            this.Size = new Size(400, 100);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.AutoScroll = true;

            btnSubmit = new Button
            {
                Text = "수정",
                DialogResult = DialogResult.OK,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };

            btnCancel = new Button
            {
                Text = "취소",
                DialogResult = DialogResult.Cancel,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };

            this.AcceptButton = btnSubmit;
            this.CancelButton = btnCancel;
        }

        private void CreateInputFields()
        {
            // 컨트롤 배치를 위한 초기 Y 좌표 (상단 여백)
            int yPos = 20;

            // 레이아웃 상수 정의
            const int padding = 10;      // 컨트롤 간의 여백
            const int labelWidth = 120;  // 레이블의 너비
            const int controlWidth = 200; // 입력 컨트롤의 너비
            const int controlHeight = 20; // 컨트롤의 높이
            const int spacing = 30;      // 다음 컨트롤까지의 세로 간격

            // columnTypes 딕셔너리를 순회하며 각 컬럼에 대한 입력 필드 생성
            foreach (var column in columnTypes)
            {
                // 1. 레이블 생성 (필드명 표시)
                var label = new Label
                {
                    Text = column.Key,   // 컬럼명을 레이블 텍스트로 사용
                    Location = new Point(padding, yPos), // 왼쪽 여백, 현재 y좌표
                    Size = new Size(labelWidth, controlHeight),
                    TextAlign = ContentAlignment.MiddleRight // 텍스트 오른쪽 정렬
                };

                // 2. 데이터 타입에 따른 입력 컨트롤 생성
                Control inputControl;
                switch (column.Value.ToLower())
                {
                    case "bit": // 불리언(참/거짓) 타입
                        inputControl = new CheckBox
                        {
                            Location = new Point(padding + labelWidth + padding, yPos),
                            Size = new Size(controlWidth, controlHeight)
                        };
                        break;

                    case "datetime": // 날짜/시간 타입
                        inputControl = new DateTimePicker
                        {
                            Location = new Point(padding + labelWidth + padding, yPos),
                            Size = new Size(controlWidth, controlHeight),
                            Format = DateTimePickerFormat.Short // 날짜만 표시
                        };
                        break;

                    case "int":
                    case "decimal":
                    case "float": // 숫자 타입
                        var numericBox = new TextBox
                        {
                            Location = new Point(padding + labelWidth + padding, yPos),
                            Size = new Size(controlWidth, controlHeight)
                        };
                        // 숫자와 소수점만 입력 가능하도록 제한
                        numericBox.KeyPress += (s, e) =>
                        {
                            // 숫자, 백스페이스, Delete 키만 허용
                            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
                            {
                                e.Handled = true; // 다른 문자 입력 차단
                            }
                            // 소수점은 한 번만 입력 가능
                            if (e.KeyChar == '.' && (s as TextBox).Text.Contains("."))
                            {
                                e.Handled = true;
                            }
                        };
                        inputControl = numericBox;
                        break;

                    default: // 문자열 타입
                        inputControl = new TextBox
                        {
                            Location = new Point(padding + labelWidth + padding, yPos),
                            Size = new Size(controlWidth, controlHeight)
                        };
                        break;
                }

                // 3. ID 컬럼인 경우 읽기 전용으로 설정
                if (column.Key == idColumnName)
                {
                    if (inputControl is TextBox textBox)
                    {
                        textBox.ReadOnly = true; // 수정 불가능하게 설정
                        textBox.BackColor = SystemColors.Control; // 읽기 전용 배경색
                    }
                }

                // 4. 기존 데이터가 있는 경우 컨트롤에 값 설정
                if (existingData.ContainsKey(column.Key))
                {
                    var value = existingData[column.Key];
                    if (inputControl is CheckBox checkBox)
                        checkBox.Checked = Convert.ToBoolean(value);
                    else if (inputControl is DateTimePicker datePicker)
                        datePicker.Value = Convert.ToDateTime(value);
                    else if (inputControl is TextBox textBox)
                        textBox.Text = value?.ToString() ?? "";
                }

                // 5. 생성된 컨트롤을 폼에 추가
                this.Controls.Add(label);
                this.Controls.Add(inputControl);
                inputControls.Add(column.Key, inputControl); // 나중에 값을 수집하기 위해 저장

                // 다음 컨트롤을 위해 Y 좌표 증가
                yPos += spacing;
            }

            // 6. 버튼 위치 설정
            btnCancel.Location = new Point(this.ClientSize.Width - padding - btnCancel.Width,
                                         yPos + padding);
            btnSubmit.Location = new Point(btnCancel.Left - padding - btnSubmit.Width,
                                         yPos + padding);

            // 7. 버튼을 폼에 추가
            this.Controls.Add(btnSubmit);
            this.Controls.Add(btnCancel);

            // 8. 폼 크기를 컨트롤에 맞게 조정
            this.ClientSize = new Size(this.ClientSize.Width,
                                     yPos + btnSubmit.Height + padding * 2);

            // 9. 제출 버튼 클릭 이벤트 핸들러 연결
            btnSubmit.Click += BtnSubmit_Click;
        }

        private void BtnSubmit_Click(object sender, EventArgs e)
        {
            if (ValidateInputs())
            {
                CollectFormData();
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private bool ValidateInputs()
        {
            foreach (var control in inputControls)
            {
                if (control.Value is TextBox textBox && 
                    string.IsNullOrWhiteSpace(textBox.Text) && 
                    control.Key != idColumnName)  // ID 필드는 검증에서 제외
                {
                    MessageBox.Show($"{control.Key} 필드는 필수입니다.", "입력 오류",
                                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    textBox.Focus();
                    return false;
                }
            }
            return true;
        }

        private void CollectFormData()
        {
            FormData = new Dictionary<string, object>();
            foreach (var control in inputControls)
            {
                object value = null;
                if (control.Value is CheckBox checkBox)
                    value = checkBox.Checked;
                else if (control.Value is DateTimePicker datePicker)
                    value = datePicker.Value;
                else if (control.Value is TextBox textBox)
                {
                    string text = textBox.Text.Trim();
                    if (columnTypes[control.Key].ToLower() == "int")
                        value = Convert.ToInt32(text);
                    else if (columnTypes[control.Key].ToLower() == "decimal")
                        value = Convert.ToDecimal(text);
                    else if (columnTypes[control.Key].ToLower() == "float")
                        value = Convert.ToSingle(text);
                    else
                        value = text;
                }
                FormData.Add(control.Key, value);
            }
        }
    }
} 