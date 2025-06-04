using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using System.Drawing;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        private readonly DatabaseManager dbManager;
        private string currentTable = "";
        private Label welcomeLabel; // 환영 메시지를 표시할 레이블
        private ToolStripComboBox tableComboBox;  // 테이블 선택 콤보박스

        // 테이블별 컬럼 타입 캐시
        private readonly Dictionary<string, Dictionary<string, string>> tableColumnTypes = new();

        public Form1()
        {
            InitializeComponent();
            InitializeTableSelector();
            dbManager = DatabaseManager.Instance;
            InitializeDatabase();
            InitializeDataGridView();
            InitializeWelcomeMessage();
        }

        private void InitializeTableSelector()
        {
            // 테이블 선택 콤보박스 생성
            tableComboBox = new ToolStripComboBox
            {
                Name = "tableSelector",
                Size = new Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,  // 직접 입력 불가능하게 설정
                ToolTipText = "테이블 선택"
            };

            // 사용 가능한 테이블 목록 동적 로드
            var tables = dbManager.GetTableNames();
            tableComboBox.Items.Add("선택하세요");
            foreach (var t in tables)
            {
                tableComboBox.Items.Add(t);
            }

            // 기본값 선택
            tableComboBox.SelectedIndex = 0;  // "선택하세요"를 기본값으로

            var btnDelete = new ToolStripButton
            {
                Text = "데이터 삭제",
                Image = System.Drawing.SystemIcons.Warning.ToBitmap(),
                ImageScaling = ToolStripItemImageScaling.SizeToFit,
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
            };
            btnDelete.Click += TableManagement_Delete;

            var btnEdit = new ToolStripButton
            {
                Text = "데이터 수정",
                Image = System.Drawing.SystemIcons.Information.ToBitmap(),
                ImageScaling = ToolStripItemImageScaling.SizeToFit,
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                Tag = "edit_mode_off"  // 수정 모드 상태 추적을 위한 태그
            };
            btnEdit.Click += TableManagement_Edit;

            // 왼쪽 영역 아이템 추가
            menuStrip1.Items.Add(new ToolStripLabel("테이블 선택:"));
            menuStrip1.Items.Add(tableComboBox);
            menuStrip1.Items.Add(new ToolStripSeparator());
            menuStrip1.Items.Add(btnEdit); // 데이터 수정 버튼을 먼저 추가
            menuStrip1.Items.Add(btnDelete);

            // 테이블 변경 이벤트 처리
            tableComboBox.SelectedIndexChanged += TableComboBox_SelectedIndexChanged;
        }

        private void TableManagement_Add(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentTable) || currentTable == "선택하세요")
            {
                MessageBox.Show("테이블을 먼저 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                if (!tableColumnTypes.ContainsKey(currentTable))
                {
                    var schema = dbManager.GetColumnTypes(currentTable);
                    if (schema.Count == 0)
                    {
                        MessageBox.Show($"{currentTable} 테이블의 스키마 정보를 가져올 수 없습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    tableColumnTypes[currentTable] = schema;
                }

                var columnTypes = tableColumnTypes[currentTable];

                // DataInputForm을 사용하여 새 데이터 입력 받기
                using (var inputForm = new DataInputForm(currentTable, columnTypes))
                {
                    if (inputForm.ShowDialog() == DialogResult.OK)
                    {
                        var data = inputForm.FormData;

                        bool success = dbManager.AddDataEntry(currentTable, data);

                        if (success)
                        {
                            MessageBox.Show("데이터가 성공적으로 추가되었습니다.", "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            LoadTableData(currentTable); // 테이블 데이터 새로고침
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"데이터 추가 중 오류가 발생했습니다.\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TableManagement_Edit(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentTable) || currentTable == "선택하세요")
            {
                MessageBox.Show("테이블을 먼저 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var editButton = (ToolStripButton)sender;
            bool isEditMode = editButton.Tag.ToString() == "edit_mode_on";

            if (!isEditMode)
            {
                // 수정 모드 시작
                dataGridView1.ReadOnly = false;
                if (dataGridView1.Columns.Count > 0)
                {
                    dataGridView1.Columns[0].ReadOnly = true;  // ID 컬럼은 항상 읽기 전용
                }
                editButton.Text = "변경사항 저장";
                editButton.Tag = "edit_mode_on";
                editButton.Image = System.Drawing.SystemIcons.Shield.ToBitmap();  // 아이콘 변경
            }
            else
            {
                try
                {
                    // 수정된 데이터 저장
                    var modifiedRows = GetModifiedRows();
                    if (modifiedRows.Count == 0)
                    {
                        MessageBox.Show("수정된 데이터가 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        int successCount = 0;
                        foreach (var row in modifiedRows)
                        {
                            string idColumnName = dataGridView1.Columns[0].Name;
                            bool success = dbManager.UpdateDataEntry(currentTable, idColumnName, row);
                            if (success) successCount++;
                        }

                        if (successCount > 0)
                        {
                            MessageBox.Show($"{successCount}개의 데이터가 성공적으로 수정되었습니다.", "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            LoadTableData(currentTable); // 테이블 데이터 새로고침
                        }
                    }

                    // 수정 모드 종료
                    dataGridView1.ReadOnly = true;
                    editButton.Text = "데이터 수정";
                    editButton.Tag = "edit_mode_off";
                    editButton.Image = System.Drawing.SystemIcons.Information.ToBitmap();  // 아이콘 원래대로
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"데이터 수정 중 오류가 발생했습니다.\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private List<Dictionary<string, object>> GetModifiedRows()
        {
            var modifiedRows = new List<Dictionary<string, object>>();
            var dataTable = (DataTable)dataGridView1.DataSource;
            
            if (dataTable == null) return modifiedRows;

            foreach (DataRow row in dataTable.Rows)
            {
                if (row.RowState == DataRowState.Modified)
                {
                    var rowData = new Dictionary<string, object>();
                    foreach (DataColumn col in dataTable.Columns)
                    {
                        rowData[col.ColumnName] = row[col];
                    }
                    modifiedRows.Add(rowData);
                }
            }

            return modifiedRows;
        }

        private void TableManagement_Delete(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentTable) || currentTable == "선택하세요")
            {
                MessageBox.Show("테이블을 먼저 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 여기에 데이터 삭제 로직 구현
            MessageBox.Show($"{currentTable} 테이블에서 데이터를 삭제합니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void TableComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedTable = tableComboBox.SelectedItem.ToString();
            
            if (selectedTable == "선택하세요")
            {
                // 선택하세요 항목 선택시 환영 메시지 표시
                dataGridView1.Visible = false;
                welcomeLabel.Visible = true;
                currentTable = "";
                return;
            }

            try
            {
                // 선택된 테이블의 데이터 로드
                LoadTableData(selectedTable);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"테이블 데이터 로드 중 오류가 발생했습니다.\n{ex.Message}",
                              "오류",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Error);
                
                // 오류 발생 시 선택 초기화
                tableComboBox.SelectedIndexChanged -= TableComboBox_SelectedIndexChanged;
                tableComboBox.SelectedIndex = 0;
                tableComboBox.SelectedIndexChanged += TableComboBox_SelectedIndexChanged;
            }
        }

        private void InitializeDatabase()
        {
            dbManager.Connect();
        }

        private void InitializeDataGridView()
        {
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.MultiSelect = false;
            dataGridView1.ReadOnly = true;  // 기본적으로 읽기 전용
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.RowTemplate.Height = 25;
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            dataGridView1.GridColor = Color.LightGray;

            // 행 헤더 숨기기
            dataGridView1.RowHeadersVisible = false;

            // 첫 번째 컬럼(ID)은 항상 수정 불가능하도록 설정
            dataGridView1.CellBeginEdit += (s, e) =>
            {
                if (e.ColumnIndex == 0) // ID 컬럼
                {
                    e.Cancel = true;
                }
            };

            // 초기에는 DataGridView 숨기기
            dataGridView1.Visible = false;

            // 마지막 행에 버튼 컬럼 추가를 위한 이벤트 핸들러
            dataGridView1.RowPostPaint += DataGridView1_RowPostPaint;
            dataGridView1.CellClick += DataGridView1_CellClick;
        }

        private void DataGridView1_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            // 마지막 행인 경우에만 + 버튼 그리기
            if (e.RowIndex == dataGridView1.Rows.Count - 1)
            {
                var bounds = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, 
                                        dataGridView1.Width - 2, e.RowBounds.Height); // 전체 너비로 변경

                // 배경을 흰색으로 채워서 기존 그리드 라인을 덮음
                using (var backBrush = new SolidBrush(Color.White))
                {
                    e.Graphics.FillRectangle(backBrush, bounds);
                }

                using (var brush = new SolidBrush(Color.FromArgb(0, 120, 215)))
                using (var pen = new Pen(brush, 2))
                {
                    var g = e.Graphics;
                    
                    // + 기호 그리기 (왼쪽에서 약간 띄움)
                    int leftMargin = 20;  // 왼쪽 여백
                    int centerX = bounds.Left + leftMargin + 15;  // + 기호 위치 조정
                    int centerY = bounds.Top + bounds.Height / 2;
                    int size = 10;  // + 기호의 크기

                    g.DrawLine(pen, centerX - size, centerY, centerX + size, centerY);  // 가로선
                    g.DrawLine(pen, centerX, centerY - size, centerX, centerY + size);  // 세로선

                    // "새 데이터 추가" 텍스트 그리기 (+ 기호 옆으로 이동)
                    using (var font = new Font("맑은 고딕", 9))
                    {
                        string text = "새 데이터 추가";
                        var textBounds = new Rectangle(centerX + size + 10, bounds.Top, 
                                                     bounds.Width - (centerX + size + 20), bounds.Height);
                        
                        g.DrawString(text, font, brush, 
                                   textBounds, 
                                   new StringFormat { Alignment = StringAlignment.Near, 
                                                    LineAlignment = StringAlignment.Center });
                    }
                }
            }
        }

        private void DataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // 마지막 행의 첫 번째 또는 두 번째 컬럼을 클릭했을 때
            if (e.RowIndex == dataGridView1.Rows.Count - 1 && (e.ColumnIndex == 0 || e.ColumnIndex == 1))
            {
                TableManagement_Add(sender, EventArgs.Empty);
            }
        }

        private void InitializeWelcomeMessage()
        {
            // 환영 메시지 레이블 생성
            welcomeLabel = new Label
            {
                Text = "환영합니다",
                Font = new Font("맑은 고딕", 36, FontStyle.Bold),
                ForeColor = Color.FromArgb(64, 64, 64),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = true  // 텍스트 크기에 맞게 자동 조정
            };

            // 폼 크기가 변경될 때마다 레이블 위치 조정
            this.SizeChanged += (s, e) => CenterWelcomeLabel();
            
            // DataGridView와 같은 부모 컨테이너에 레이블 추가
            dataGridView1.Parent.Controls.Add(welcomeLabel);
            welcomeLabel.BringToFront();
            
            // 초기 위치 설정
            CenterWelcomeLabel();
        }

        private void CenterWelcomeLabel()
        {
            if (welcomeLabel != null)
            {
                // 폼의 작업 영역 (메뉴바 제외) 가져오기
                Rectangle clientRect = this.ClientRectangle;
                
                // 레이블을 폼의 중앙보다 약간 위쪽에 배치 (5%정도 위로)
                int yOffset = (int)(clientRect.Height * 0.03);  // 5% 위로 올림
                
                welcomeLabel.Left = (clientRect.Width - welcomeLabel.Width) / 2;
                welcomeLabel.Top = (clientRect.Height - welcomeLabel.Height) / 2 - yOffset;

                // 최소 상단 여백 확보
                if (welcomeLabel.Top < 20)
                {
                    welcomeLabel.Top = 20;
                }
            }
        }

        private void LoadTableData(string tableName)
        {
            currentTable = tableName;
            DataTable data = new DataTable();
            
            data = dbManager.GetTableData(tableName);
            
            if (data != null)
            {
                // 데이터를 로드할 때 환영 메시지 숨기기
                welcomeLabel.Visible = false;
                dataGridView1.DataSource = data;

                // 첫 번째 컬럼(ID)의 너비 설정
                if (dataGridView1.Columns.Count > 0)
                {
                    dataGridView1.Columns[0].Width = 50;
                    dataGridView1.Columns[0].ReadOnly = true;  // ID 컬럼은 읽기 전용

                    // 나머지 컬럼들은 자동 조정
                    for (int i = 1; i < dataGridView1.Columns.Count; i++)
                    {
                        dataGridView1.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    }
                }

                // 빈 행 하나 추가 (+ 버튼을 위한 공간)
                var emptyRow = data.NewRow();
                data.Rows.Add(emptyRow);

                // 마지막 행의 셀 스타일 설정
                dataGridView1.Rows[dataGridView1.Rows.Count - 1].DefaultCellStyle.BackColor = Color.White;
                dataGridView1.Rows[dataGridView1.Rows.Count - 1].DefaultCellStyle.SelectionBackColor = Color.White;
                dataGridView1.Rows[dataGridView1.Rows.Count - 1].DefaultCellStyle.ForeColor = Color.FromArgb(0, 120, 215);
                dataGridView1.Rows[dataGridView1.Rows.Count - 1].DefaultCellStyle.SelectionForeColor = Color.FromArgb(0, 120, 215);

                dataGridView1.Visible = true;

                // 콤보박스 선택 업데이트
                if (tableComboBox.SelectedItem.ToString() != tableName)
                {
                    tableComboBox.SelectedIndexChanged -= TableComboBox_SelectedIndexChanged;
                    tableComboBox.SelectedItem = tableName;
                    tableComboBox.SelectedIndexChanged += TableComboBox_SelectedIndexChanged;
                }
            }
        }


        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            dbManager.Disconnect();
        }
    }
} 