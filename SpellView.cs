using Godot;
using System;
using System.Collections.Generic;

public partial class SpellView : Panel
{

	private Label vocabulary;
	private Label posTag;
	private Label status;
	private LineEdit input;
	private Button submitButton;
	private string trueSpell;
	private int currentIndex;
	private List<int> randomOrder = new List<int>();
	private int randomPointer = 0;
	public override void _Ready()
	{
		vocabulary = GetNodeOrNull<Label>("vocabulary");
		posTag = GetNodeOrNull<Label>("posTagging");
		status = GetNodeOrNull<Label>("status");
		input = GetNodeOrNull<LineEdit>("input");
		submitButton = GetNodeOrNull<Button>("submitButton");
		if (vocabulary == null || posTag == null || status == null || input == null || submitButton == null)
		{
			GD.PrintErr("SpellView节点获取失败");
			return;
		}
		submitButton.Pressed += Submit;
		PrepareRandomOrder();
		LoadWordList();
	}
	public override void _Process(double delta)
	{
	}
	private void Submit()
	{
		if (submitButton.Text == "Submit")
		{
			GD.Print("Submit pressed");
			submitButton.Text = "Confirm";
			AnswerCheck();
			return;
		}
		else if (submitButton.Text == "Confirm")
		{
			submitButton.Text = "Submit";
			status.Text = "✅❌确认后显示正确答案";
			input.Text = "";
			LoadWordList();
		}
	}
	private void AnswerCheck()
	{
		if (trueSpell == input.Text)
		{
			status.Text = "✅正确答案:" + trueSpell;
		}
		else
		{
			status.Text = "❌错误答案:" + trueSpell;
			// 答案错误时，数据库wrongCount+1
			string rootPath = ProjectSettings.GlobalizePath("res://");
			string wordDir = System.IO.Path.Combine(rootPath, "word");
			string dbPath = System.IO.Path.Combine(wordDir, "book.sqlite");
			using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
			{
				connection.Open();
				string updateCmd = "UPDATE Words SET wrongCount = wrongCount + 1 WHERE Vocabulary = @Vocabulary AND Spell = @Spell;";
				using (var command = connection.CreateCommand())
				{
					command.CommandText = updateCmd;
					command.Parameters.AddWithValue("@Vocabulary", vocabulary.Text);
					command.Parameters.AddWithValue("@Spell", trueSpell);
					command.ExecuteNonQuery();
				}
			}
		}
	}
	private void LoadWordList()
	{
		string rootPath = ProjectSettings.GlobalizePath("res://");
		string wordDir = System.IO.Path.Combine(rootPath, "word");
		string dbPath = System.IO.Path.Combine(wordDir, "book.sqlite");
		int totalCount = 0;
		using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
		{
			connection.Open();
			string countCmd = "SELECT COUNT(*) FROM Words;";
			using (var countCommand = connection.CreateCommand())
			{
				countCommand.CommandText = countCmd;
				totalCount = Convert.ToInt32(countCommand.ExecuteScalar());
			}
		}
		if (totalCount == 0)
		{
			vocabulary.Text = "";
			posTag.Text = "";
			trueSpell = "";
			return;
		}
		// 初始化或重置随机序列
		if (randomOrder.Count != totalCount || randomPointer >= randomOrder.Count)
		{
			PrepareRandomOrder();
		}
		int offset = randomOrder[randomPointer];
		randomPointer++;

		using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
		{
			connection.Open();
			string selectCmd = "SELECT Vocabulary, PosTag, Spell FROM Words ORDER BY [Index] ASC LIMIT 1 OFFSET @Offset;";
			using (var command = connection.CreateCommand())
			{
				command.CommandText = selectCmd;
				command.Parameters.AddWithValue("@Offset", offset);
				using (var reader = command.ExecuteReader())
				{
					if (reader.Read())
					{
						vocabulary.Text = reader.GetString(0);
						posTag.Text = reader.GetString(1);
						trueSpell = reader.GetString(2);
					}
					else
					{
						vocabulary.Text = "";
						posTag.Text = "";
						trueSpell = "";
					}
				}
			}
		}
	}

	private void PrepareRandomOrder()
	{
		string rootPath = ProjectSettings.GlobalizePath("res://");
		string wordDir = System.IO.Path.Combine(rootPath, "word");
		string dbPath = System.IO.Path.Combine(wordDir, "book.sqlite");
		int totalCount = 0;
		using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
		{
			connection.Open();
			string countCmd = "SELECT COUNT(*) FROM Words;";
			using (var countCommand = connection.CreateCommand())
			{
				countCommand.CommandText = countCmd;
				totalCount = Convert.ToInt32(countCommand.ExecuteScalar());
			}
		}
		randomOrder.Clear();
		for (int i = 0; i < totalCount; i++)
		{
			randomOrder.Add(i);
		}
		// 洗牌算法
		Random rng = new Random();
		int n = randomOrder.Count;
		while (n > 1)
		{
			n--;
			int k = rng.Next(n + 1);
			int value = randomOrder[k];
			randomOrder[k] = randomOrder[n];
			randomOrder[n] = value;
		}
		randomPointer = 0;
	}
}
