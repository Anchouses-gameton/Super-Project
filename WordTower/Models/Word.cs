using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WordTower.Models;

public class Word
{
    public int Id { get; set; }
    public string Text { get; set; }

    public Word(int id, string text)
    {
        Id = id;
        Text = text;
    }
}
