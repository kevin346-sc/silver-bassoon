clear;clc;
delete(instrfind({'Port'},{'COM3'}));%断开，释放串口对象占用空间
global s;%建立全局变量s
s = serial('COM3','BaudRate',9600,'DataBits',8);%设定相关的串口参数
fopen(s);
set(s,'BytesAvailableFcnMode','Terminator');
set(s,'Terminator','H');                    %设立中断，当串口检测到字符H时触发中断
s.BytesAvailableFcn =@ReceiveCallback; %中断触发后处理回调的事

pause(1);
str="5,4,6,7,H";
fprintf(s,str);
pause(0.1);%时间间隔不宜过小
    
function ReceiveCallback(obj,event) %执行回调的函数
global s;
a = fscanf(s);
disp(a);
I = 'I Received';
disp(I);
end