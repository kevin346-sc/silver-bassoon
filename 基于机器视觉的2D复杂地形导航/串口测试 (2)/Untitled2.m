clear;clc;
delete(instrfind({'Port'},{'COM8'}));%断开，释放串口对象占用空间
scom = serial('COM8');

scom.InputBufferSize =512;
scom.OutputBufferSize =512;
scom.ReadAsyncMode='continuous';
scom.BaudRate =9600;
scom.Parity ='none';
scom.StopBits =1;
scom.DataBits =8;
scom.Terminator ='CR';
scom.FlowControl ='none';
scom.timeout =1;
scom.BytesAvailableFcnMode = 'byte';
scom.BytesAvailableFcnCount = 1024;
scom.BytesAvailableFcn = @callback;
scom.BytesAvailableFcn =@ReceiveCallback; %中断触发后处理回调的事

fopen(scom);

str = '1,2,3,4,5,CR,';
for k=1:1:10
    fprintf(scom,str); %以字符(ASCII码)形式向串口写数据str(字符或字符串)
    pause(0.1);%时间间隔不宜过小
end


function ReceiveCallback(obj,event) %执行回调的函数
global scom;
a = fscanf(scom);
disp(a);
I = 'I Received';
disp(I);
end