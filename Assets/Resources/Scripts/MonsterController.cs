using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface INode
{
    public enum BTState
    { RUN, SUCCESS, FAILED }

    public BTState Evaluate(); // 판단하여 상태 리턴
}

public class ActionNode : INode
{
    public Func<INode.BTState> action;

    public ActionNode(Func<INode.BTState> action) // 실행할 동작
    {
        this.action = action;
    }

    public INode.BTState Evaluate()
    {
        // 대리자가 null 이 아닐 때 호출, null 인 경우 Failed 반환
        return action?.Invoke() ?? INode.BTState.FAILED;
    }
}

public class SelectorNode : INode
{
    List<INode> children; // 여러 노드를 가질 수 있도록 리스트 생성

    public SelectorNode() { children = new List<INode>(); }

    public void Add(INode node) { children.Add(node); } // 셀렉터에 자식노드를 추가하는 메서드

    public INode.BTState Evaluate()
    {
        // 리스트 내의 노드들을 왼쪽부터(넣은 순으로) 검사
        foreach (INode child in children)
        {

            INode.BTState state = child.Evaluate();
            // child 노드의 state 가 하나라도 SUCCESS 이면 성공을 반환
            // 실행 중인 경우 RUN 반환
            switch (state)
            {
                case INode.BTState.SUCCESS:
                    return INode.BTState.SUCCESS;
                case INode.BTState.RUN:
                    return INode.BTState.RUN;
            }
        }
        // 반복문이 끝났다면 해당 셀렉터의 자식노드들은 전부 FAILED 상태이므로 셀렉터는 FAILED 반환
        return INode.BTState.FAILED;
    }
}

public class SequenceNode : INode
{
    List<INode> children; // 자식 노드들을 담을 수 있는 리스트

    public SequenceNode() { children = new List<INode>(); }

    public void Add(INode node) { children.Add(node); }

    public INode.BTState Evaluate()
    {
        // 자식 노드의 수가 0 이하라면 실패
        if (children.Count <= 0)
            return INode.BTState.FAILED;

        foreach (INode child in children)
        {
            // 자식 노드들중 하나라도 FAILED 라면 시퀀스는 FAILED
            switch (child.Evaluate())
            {
                case INode.BTState.RUN:
                    return INode.BTState.RUN;
                case INode.BTState.SUCCESS:
                    continue;   // 다음 노드 실행
                case INode.BTState.FAILED:
                    return INode.BTState.FAILED;
            }
        }
        // FAILED 에 걸리지 않고 반복문을 빠져나왔으므로 시퀀스는 SUCCESS
        return INode.BTState.SUCCESS;
    }
}

public class MonsterController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
