using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestionNode : ITreeNode
{
    Func<bool> _question;
    ITreeNode _fNode;
    ITreeNode _tNode;
    public QuestionNode(Func<bool> question, ITreeNode tNode, ITreeNode fNode)
    {
        _question = question;
        _tNode = tNode;
        _fNode = fNode;
    }

    public void Execute()
    {
        if (_question())
        {
            _tNode.Execute();
        }
        else
        {
            _fNode.Execute();
        }
    }
}
