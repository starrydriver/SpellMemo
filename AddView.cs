using Godot;
using System;
using Microsoft.Data.Sqlite;
public partial class AddView : Panel
{
	private LineEdit vocabularyInput;
	private LineEdit posTagInput;
	private LineEdit spellInput;
	private Button addButton;
	private Label statusLabel;
	private int dbIndex;
	public override void _Ready()
	{
		CreateWordList();
		vocabularyInput = GetNodeOrNull<LineEdit>("input_vocabulary");
		posTagInput = GetNodeOrNull<LineEdit>("input_pos");
		spellInput = GetNodeOrNull<LineEdit>("input_spell");
		addButton = GetNodeOrNull<Button>("Button");
		statusLabel = GetNodeOrNull<Label>("status");
		if (vocabularyInput == null || posTagInput == null || spellInput == null || addButton == null || statusLabel == null)
		{
			GD.PrintErr("AddView节点获取失败");
			return;
		}
		addButton.Pressed += AddWord;
		vocabularyInput.TextChanged += (string newText) => ConfirmReset();
		posTagInput.TextChanged += (string newText) => ConfirmReset();
		spellInput.TextChanged += (string newText) => ConfirmReset();
	}
	public override void _Process(double delta)
	{
	}
	private void AddWord()
	{
		if (addButton.Text == "Add")
		{
			addButton.Text = "Confirm";
			return;
		}
		else if (addButton.Text == "Confirm")
		{
			if (AddCheck())
			{
				AddWordToDb();
				statusLabel.Text = "✅单词添加成功";
			}
			else
			{
				statusLabel.Text = "❌单词已存在";
			}
			vocabularyInput.Text = "";
			posTagInput.Text = "";
			spellInput.Text = "";
			addButton.Text = "Add";
		}
		
	}
	private void AddWordToDb()
	{
		string rootPath = ProjectSettings.GlobalizePath("res://");
		string wordDir = System.IO.Path.Combine(rootPath, "word");
		string dbPath = System.IO.Path.Combine(wordDir, "book.sqlite");
		string vocabulary = vocabularyInput.Text;
		string posTag = posTagInput.Text;
		string spell = spellInput.Text;

		using (var connection = new SqliteConnection($"Data Source={dbPath}"))
		{
			connection.Open();
			string insertCmd = @"
				INSERT INTO Words (Vocabulary, PosTag, Spell)
				VALUES (@Vocabulary, @PosTag, @Spell);";
			using (var command = connection.CreateCommand())
			{
				command.CommandText = insertCmd;
				command.Parameters.AddWithValue("@Vocabulary", vocabulary);
				command.Parameters.AddWithValue("@PosTag", posTag);
				command.Parameters.AddWithValue("@Spell", spell);
				command.ExecuteNonQuery();
			}
		}
	}
	private bool AddCheck()
	{
		string rootPath = ProjectSettings.GlobalizePath("res://");
		string wordDir = System.IO.Path.Combine(rootPath, "word");
		string dbPath = System.IO.Path.Combine(wordDir, "book.sqlite");
		string vocabulary = vocabularyInput.Text;
		string spell = spellInput.Text;

		using (var connection = new SqliteConnection($"Data Source={dbPath}"))
		{
			connection.Open();
			string queryCmd = @"
				SELECT COUNT(*) FROM Words
				WHERE Vocabulary = @Vocabulary AND Spell = @Spell;";
			using (var command = connection.CreateCommand())
			{
				command.CommandText = queryCmd;
				command.Parameters.AddWithValue("@Vocabulary", vocabulary);
				command.Parameters.AddWithValue("@Spell", spell);
				int count = Convert.ToInt32(command.ExecuteScalar());
				return count == 0;
			}
		}
	}
	private void ConfirmReset()
	{
		addButton.Text = "Add";
	}
	private void CreateWordList()
	{
		string rootPath = ProjectSettings.GlobalizePath("res://");
		string wordDir = System.IO.Path.Combine(rootPath, "word");
		string dbPath = System.IO.Path.Combine(wordDir, "book.sqlite");
		if (!System.IO.Directory.Exists(wordDir))
		{
			System.IO.Directory.CreateDirectory(wordDir);
		}
		if (!System.IO.File.Exists(dbPath))
		{
			using (var connection = new SqliteConnection($"Data Source={dbPath}"))
			{
				connection.Open();
				string createTableCmd = @"
					CREATE TABLE IF NOT EXISTS Words (
						[Index] INTEGER PRIMARY KEY AUTOINCREMENT,
						Vocabulary TEXT,
						PosTag TEXT,
						Spell TEXT
					);";
				using (var command = connection.CreateCommand())
				{
					command.CommandText = createTableCmd;
					command.ExecuteNonQuery();
				}
			}
		}
		using (var connection = new SqliteConnection($"Data Source={dbPath}"))
		{
			connection.Open();
			string countCmd = "SELECT COUNT(*) FROM Words;";
			using (var command = connection.CreateCommand())
			{
				command.CommandText = countCmd;
				dbIndex = Convert.ToInt32(command.ExecuteScalar());
			}
		}
		using (var connection = new SqliteConnection($"Data Source={dbPath}"))
        {
            connection.Open();
            // 检查wrongCount字段是否存在
            string checkColumnCmd = "PRAGMA table_info(Words);";
            bool hasWrongCount = false;
            using (var command = connection.CreateCommand())
            {
                command.CommandText = checkColumnCmd;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader.GetString(1) == "wrongCount")
                        {
                            hasWrongCount = true;
                            break;
                        }
                    }
                }
            }
            // 如果没有wrongCount字段，则添加
            if (!hasWrongCount)
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "ALTER TABLE Words ADD COLUMN wrongCount INTEGER DEFAULT 0;";
                    command.ExecuteNonQuery();
                }
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "UPDATE Words SET wrongCount = 0 WHERE wrongCount IS NULL;";
                    command.ExecuteNonQuery();
                }
            }
        }
	}
}