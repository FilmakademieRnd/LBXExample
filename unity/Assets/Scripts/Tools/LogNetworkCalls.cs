using System.Text;
using UnityEngine;

public class LogNetworkCalls : MonoBehaviour{

    public static bool logCalls = false;
    public const string RCPPara_Call = "<color=yellow>";            //Sender calls an RCP, e.g. m_askOthers.Call(0)
    public const string RCPPara_Trigger = "<color=#FF9933>";         //Receiver executes the outcome of a Sender-Call, e.g. OtherReply()
    public const string RCPPara_FunctionFlow = "<color=#BAC74A>";     //this is just a local function call, which will be execute from the above but has no network relation
    public const string RCPPara_AmbiguousFunction = "<color=red>";  //can be called from function flow, via sender trigger or locally... 
    public const string COLOR_END = "</color>";
    
    //Calls Debug.Log from origin, so we can open the correct location from the console

    public static string LogCallFromSender(System.Reflection.MethodBase functionWeCallFrom, string gameObjectName, string rcpParamName, string additionalMsg = ""){
        if(!logCalls)   return "";
        StringBuilder stringBuilder = new StringBuilder(RCPPara_Call);
        stringBuilder.Append("[");
        stringBuilder.Append(functionWeCallFrom.ReflectedType.Name);    //Class Name
        stringBuilder.Append(".");
        stringBuilder.Append(functionWeCallFrom.Name);                  //Function Name from where we get called
        stringBuilder.Append("@");
        stringBuilder.Append(gameObjectName);
        stringBuilder.Append("]:");
        stringBuilder.Append(rcpParamName);                             //Name of the RCPParameter we call in the original function
        stringBuilder.Append(" <i>");
        stringBuilder.Append(additionalMsg);
        stringBuilder.Append("</i>");
        stringBuilder.Append(COLOR_END);
        return stringBuilder.ToString();
    }

    public static string LogCallAtReceiver(System.Reflection.MethodBase functionWeCallFrom, string gameObjectName, string additionalMsg = ""){
        if(!logCalls)   return "";
        StringBuilder stringBuilder = new StringBuilder(RCPPara_Trigger);
        stringBuilder.Append(" [");
        stringBuilder.Append(functionWeCallFrom.ReflectedType.Name);    //Class Name
        stringBuilder.Append(".");
        stringBuilder.Append(functionWeCallFrom.Name);                  //Function Name from where we get called (that was triggered by a call from sender)
        stringBuilder.Append("@");
        stringBuilder.Append(gameObjectName);
        stringBuilder.Append("] <i>");
        stringBuilder.Append(additionalMsg);
        stringBuilder.Append("</i>");
        stringBuilder.Append(COLOR_END);
        return stringBuilder.ToString();
    }

    public static string LogFunctionFlowCall(System.Reflection.MethodBase functionWeCallFrom, string gameObjectName, string additionalMsg = ""){
        if(!logCalls)   return "";
        StringBuilder stringBuilder = new StringBuilder(RCPPara_FunctionFlow);
        stringBuilder.Append(" [");
        stringBuilder.Append(functionWeCallFrom.ReflectedType.Name);    //Class Name
        stringBuilder.Append(".");
        stringBuilder.Append(functionWeCallFrom.Name);                  //Function Name from where we get called (that was triggered by a call from sender)
        stringBuilder.Append("@");
        stringBuilder.Append(gameObjectName);
        stringBuilder.Append("] <i>");
        stringBuilder.Append(additionalMsg);
        stringBuilder.Append("</i>");
        stringBuilder.Append(COLOR_END);
        return stringBuilder.ToString();
    }

    public static string LogAmbiguousFunction(System.Reflection.MethodBase functionWeCallFrom, string gameObjectName, string additionalMsg = ""){
        if(!logCalls)   return "";
        StringBuilder stringBuilder = new StringBuilder(RCPPara_AmbiguousFunction);
        stringBuilder.Append(" [");
        stringBuilder.Append(functionWeCallFrom.ReflectedType.Name);    //Class Name
        stringBuilder.Append(".");
        stringBuilder.Append(functionWeCallFrom.Name);                  //Function Name from where we get called (that was triggered by a call from sender)
        stringBuilder.Append("@");
        stringBuilder.Append(gameObjectName);
        stringBuilder.Append("] <i>");
        stringBuilder.Append(additionalMsg);
        stringBuilder.Append("</i>");
        stringBuilder.Append(COLOR_END);
        return stringBuilder.ToString();
    }
}
