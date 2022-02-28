function varargout = A(varargin)
% A MATLAB code for A.fig
%      A, by itself, creates a new A or raises the existing
%      singleton*.
%
%      H = A returns the handle to a new A or the handle to
%      the existing singleton*.
%
%      A('CALLBACK',hObject,eventData,handles,...) calls the local
%      function named CALLBACK in A.M with the given input arguments.
%
%      A('Property','Value',...) creates a new A or raises the
%      existing singleton*.  Starting from the left, property value pairs are
%      applied to the GUI before A_OpeningFcn gets called.  An
%      unrecognized property name or invalid value makes property application
%      stop.  All inputs are passed to A_OpeningFcn via varargin.
%
%      *See GUI Options on GUIDE's Tools menu.  Choose "GUI allows only one
%      instance to run (singleton)".
%
% See also: GUIDE, GUIDATA, GUIHANDLES
gui_Singleton = 1;
gui_State = struct('gui_Name',       mfilename, ...
                   'gui_Singleton',  gui_Singleton, ...
                   'gui_OpeningFcn', @A_OpeningFcn, ...
                   'gui_OutputFcn',  @A_OutputFcn, ...
                   'gui_LayoutFcn',  [] , ...
                   'gui_Callback',   []);
if nargin && ischar(varargin{1})
    gui_State.gui_Callback = str2func(varargin{1});
end

if nargout
    [varargout{1:nargout}] = gui_mainfcn(gui_State, varargin{:});
else
    gui_mainfcn(gui_State, varargin{:});
end
% End initialization code - DO NOT EDIT


% --- Executes just before A is made visible.
function A_OpeningFcn(hObject, eventdata, handles, varargin)
% This function has no output args, see OutputFcn.
% hObject    handle to figure
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)
% varargin   command line arguments to A (see VARARGIN)

% Choose default command line output for A
handles.output = hObject;

% Update handles structure
guidata(hObject, handles);
global map;
global source;
global goal;
global map1;
global path_new
global str;
global s;
global a;
sorce=[];
map=[];


% UIWAIT makes A wait for user response (see UIRESUME)
% uiwait(handles.figure1);


% --- Outputs from this function are returned to the command line.
function varargout = A_OutputFcn(hObject, eventdata, handles) 
% varargout  cell array for returning output args (see VARARGOUT);
% hObject    handle to figure
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)

% Get default command line output from handles structure
varargout{1} = handles.output;


% --- Executes on button press in pushbutton1.
function pushbutton1_Callback(hObject, eventdata, handles)
global map
% map=im2bw(imread('map3.bmp'))
cam=webcam(2);
map=snapshot(cam);
clear cam
% [c,s]=wavedec2(map,2,'sym3');
% cA1=appcoef2(c,s,'sym3',2);%尺度1的低频系数
% [cH1,cV1,cD1]=detcoef2('all',c,s,1);%尺度1的所有方向的高频系数H水平，V垂直，D对角
% cA1=wcodemat(cA1,192,'mat',0); %对矩阵进行量化编码
% map=cA1;
axes(handles.axes1)
imshow(map);title('拍照')


function edit1_CreateFcn(hObject, eventdata, handles)
% hObject    handle to edit1 (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    empty - handles not created until after all CreateFcns called

% Hint: edit controls usually have a white background on Windows.
%       See ISPC and COMPUTER.
if ispc && isequal(get(hObject,'BackgroundColor'), get(0,'defaultUicontrolBackgroundColor'))
    set(hObject,'BackgroundColor','white');
end


% --- Executes on button press in pushbutton15.
function pushbutton15_Callback(hObject, eventdata, handles)
axes(handles.axes1); %清空坐标轴1
cla reset;
set(handles.text4,'string','');
set(handles.text3,'string','');
set(handles.text2,'string','');
set(handles.text5,'string','');
set(handles.text8,'string','');
clear;


% --- Executes on button press in pushbutton13.
function pushbutton13_Callback(hObject, eventdata, handles)
global map 
global map1
global source
global goal
global path_new
global str
se1=strel('disk',10)
map1=imerode(map,se1);%膨胀图像

stepsize=20;    % size of each step of the RRT 步长
disTh=20;       % nodes closer than this threshold are taken as almost the same 比这个阈值更近的节点被认为是几乎相同的
maxFailedAttempts=1000; %最大迭代次数
display=true;   % display of RRT
RRT_connect=0; %双向rrt开关
APF_MODE=0; %人工势场开关
DIJ_MODE=1;%Dijkstra优化开关
APF_a=2; %引力系数
APF_amax=1 ;%引力最大值
APF_r=5;%斥力系数
APF_po=100;%斥力作用距离
tic;


if ~feasiblePoint(source,map1), error('source lies on an obstacle or outside map'); end %起点在地图外面结束
if ~feasiblePoint(goal,map1), error('goal lies on an obstacle or outside map'); end %终点在地图外面结束
if display, axes(handles.axes1),imshow(map);rectangle('position',[1 1 size(map1)-1],'edgecolor','k'); end

RRTree1 = double([source -1]); % First RRT rooted at the source, representation node and parent index 首先，RRT以源、表示节点和父索引为根
RRTree2 = double([goal -1]);   % Second RRT rooted at the goal, representation node and parent index 第二个RRT以目标、表示节点和父索引为根
counter=0; %计数器等于0
tree1ExpansionFail = false; % sets to true if expansion after set number of attempts fails 如果在设置尝试次数后的扩展失败，则设置为true
tree2ExpansionFail = false; % sets to true if expansion after set number of attempts fails

n=0;
for i=1:1:length(map1(:,1))
    for j=1:1:length(map1(1,:))
        if(map1(i,j))==0
            n=n+1;
            obs_map(n,1)=i;
            cobs_map(n,2)=j;
        end
    end
end

while ~tree1ExpansionFail || ~tree2ExpansionFail  % loop to grow RRTs 循环种植RRTs
    if ~tree1ExpansionFail
        [RRTree1,pathFound,tree1ExpansionFail] = rrtExtend(RRTree1,RRTree2,goal,obs_map,stepsize,maxFailedAttempts,disTh,map1,APF_MODE,APF_a,APF_amax,APF_r,APF_po); % RRT 1 expands from source towards goal  RRT 1从源扩展到目标
        if ~tree1ExpansionFail && isempty(pathFound) && display
            line([RRTree1(end,2);RRTree1(RRTree1(end,3),2)],[RRTree1(end,1);RRTree1(RRTree1(end,3),1)],'color','b');
            counter=counter+1;M(counter)=getframe;
        end
    end
    if RRT_connect
        if ~tree2ExpansionFail
            [RRTree2,pathFound,tree2ExpansionFail] = rrtExtend(RRTree2,RRTree1,source,obs_map,stepsize,maxFailedAttempts,disTh,map1,APF_MODE,APF_a,APF_amax,APF_r,APF_po); % RRT 2 expands from goal towards source
            if ~isempty(pathFound), pathFound(3:4)=pathFound(4:-1:3); end % path found
            if ~tree2ExpansionFail && isempty(pathFound) && display
                line([RRTree2(end,2);RRTree2(RRTree2(end,3),2)],[RRTree2(end,1);RRTree2(RRTree2(end,3),1)],'color','r');
             counter=counter+1;M(counter)=getframe;
            end
        end
    end
    if ~isempty(pathFound) % path found
         if display
            line([RRTree1(pathFound(1,3),2);pathFound(1,2);RRTree2(pathFound(1,4),2)],[RRTree1(pathFound(1,3),1);pathFound(1,1);RRTree2(pathFound(1,4),1)],'color','green');
            counter=counter+1;M(counter)=getframe;
        end
        path=[pathFound(1,1:2)]; % compute path
        prev=pathFound(1,3);     % add nodes from RRT 1 first
        while prev > 0
            path=[RRTree1(prev,1:2);path];
            prev=RRTree1(prev,3);
        end
        prev=pathFound(1,4); % then add nodes from RRT 2
        while prev > 0
            path=[path;RRTree2(prev,1:2)];
            prev=RRTree2(prev,3);
        end
        break;
    end
end

if size(pathFound,1)<=0, error('no path found. maximum attempts reached'); end %最大迭代次数后没找到终点结束
pathLength=0; %输出的路径长是0
for i=1:length(path)-1, pathLength=pathLength+distanceCost(path(i,1:2),path(i+1,1:2)); end %输出路径
fprintf('processing time=%d \nPath Length=%d \n\n', toc,pathLength); %最终路径长度
axes(handles.axes1)
imshow(map);
% rectangle('position',[1 1 size(map1)-1],'edgecolor','k');
% line(path(:,2),path(:,1));                                              
if DIJ_MODE
    [path_dis,path_new]=Dijkstra(path,map1);
    fprintf('processing time=%d \nPath Length=%d \n', toc,path_dis);
    line(path_new(:,2),path_new(:,1),'color','r');
end

path_new(:,[1 2])=path_new(:,[2 1]);
FA=path_new(:);
FA=FA';
str=num2str(FA(1));
for ii = 2:length(FA)
    str = [str,',',num2str(FA(ii))];
end
str = [str,','];
str = ['g',str];
path_new=num2str(path_new);
set(handles.text8,'String',path_new);

% --- Executes on button press in pushbutton14.
function pushbutton14_Callback(hObject, eventdata, handles)
global str
global s

pause(1);
fprintf(s,str);
pause(0.1);%时间间隔不宜过小

    

% --- Executes on button press in pushbutton23.
function pushbutton23_Callback(hObject, eventdata, handles)
% hObject    handle to pushbutton23 (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)
global source
global map
axes(handles.axes1)
imshow(map);
[y x]=ginput(1);
x=round(x)
y=round(y)
source=[x y];
set(handles.text2,'String',y)
set(handles.text3,'String',x)

% --- Executes on button press in pushbutton12.
function pushbutton12_Callback(hObject, eventdata, handles)
global goal
global map
axes(handles.axes1)
imshow(map);
[y x]=ginput(1);
x=round(x)
y=round(y)
goal=[x y];
set(handles.text4,'String',y)
set(handles.text5,'String',x)




% --- Executes on button press in pushbutton10.
function pushbutton10_Callback(hObject, eventdata, handles)
global map
thresh=graythresh(map);
map=im2bw(map,thresh);
% map=medfilt2(map,[3,3])
axes(handles.axes1)

imshow(map);
map=imresize(map,0.3,'nearest')
imwrite(map,'abc.bmp','bmp')
map=im2bw(imread('abc.bmp'))
title("二值化及滤波后")




%rrtExtend子程序
function [RRTree1,pathFound,extendFail] = rrtExtend(RRTree1,RRTree2,goal,obs_map,stepsize,maxFailedAttempts,disTh,map,APF_MODE,APF_a,APF_amax,APF_r,APF_po)
pathFound=[]; %if path found, returns new node connecting the two trees, index of the nodes in the two trees connected
failedAttempts=0;
while failedAttempts <= maxFailedAttempts
    if rand < 0.8,
        sample = rand(1,2) .* size(map); % random sample 
    else
        sample = goal;  % sample taken as goal to bias tree generation to goal 以样本为目标生成偏置树，以目标为目标
    end
     
    [A, I] = min( distanceCost(RRTree1(:,1:2),sample) ,[],1); % find the minimum value of each column 求每一列的最小值
    closestNode = RRTree1(I(1),:);
     
    %% moving from qnearest an incremental distance in the direction of qrand
    theta = atan2((sample(2)-closestNode(2)),(sample(1)-closestNode(1)));  % direction to extend sample to produce new node 指示扩展样本以产生新节点
    if APF_MODE == true
        angle=APF_angle(closestNode,goal,obs_map,length(obs_map));
        n=length(angle);
        
        [Fatx,Faty]=APF_attract(closestNode,goal,APF_a,angle(1,:),APF_amax);
        [Frerxx,Freryy]=APF_repulsion(closestNode,obs_map,APF_r,angle(2:n),n-1,APF_po);
        Fsumyj=Faty+Freryy;%y方向的合力
        Fsumxj=Fatx+Frerxx;%x方向的合力
        angle_apf=atan2(Fsumyj,Fsumxj);%合力与x轴方向的夹角向量
        newPoint = double(int32(closestNode(1:2) + stepsize * ([cos(theta) sin(theta)]+ [cos(angle_apf) sin(angle_apf)]) ));
    
    else
        newPoint = double(int32(closestNode(1:2) + stepsize * [cos(theta) sin(theta)]));
    end
    if ~checkPath(closestNode(1:2), newPoint, map) % if extension of closest node in tree to the new point is feasible
        failedAttempts = failedAttempts + 1;
        continue;
    end
     
    [A, I2] = min( distanceCost(RRTree2(:,1:2),newPoint) ,[],1); % find closest in the second tree 在第二棵树中找到最近的
    if distanceCost(RRTree2(I2(1),1:2),newPoint) < disTh,        % if both trees are connected 如果两棵树相连
        pathFound=[newPoint I(1) I2(1)];extendFail=false;break;
    end
    [A, I3] = min( distanceCost(RRTree1(:,1:2),newPoint) ,[],1); % check if new node is not already pre-existing in the tree 检查树中是否已经存在新节点
    if distanceCost(newPoint,RRTree1(I3(1),1:2)) < disTh, failedAttempts=failedAttempts+1;continue; end
    RRTree1 = [RRTree1;newPoint I(1)];extendFail=false;break; % add node 添加节点
end


%Dijkstra优化算法子程序
function [mydistance,path_new]=Dijkstra(path_raw,map) 
    n=length(path_raw); 
    sb=1;db=n;%起点终点
    a=zeros(n);%定义n阶零矩阵
    for i=1:n
        for j=1:n
            if checkPath(path_raw(i,:),path_raw(j,:),map)
                a(i,j)=distanceCost(path_raw(i,:),path_raw(j,:));
            else
                a(i,j)=inf;
            end
        end
    end
    for i=1:n
        a(i,i)=0;
    end
    visited(1:n) = 0;
    distance(1:n) = inf; % 保存起点到各顶点的最短距离
    distance(sb) = 0; parent(1:n) = 0;
    for i = 1: n-1
        temp=distance;
        id1=find(visited==1); %查找已经标号的点
        temp(id1)=inf; %已标号点的距离换成无穷
        [t, u] = min(temp); %找标号值最小的顶点
        visited(u) = 1; %标记已经标号的顶点
        id2=find(visited==0); %查找未标号的顶点
        for v = id2
            if a(u, v) + distance(u) < distance(v)
                distance(v) = distance(u) + a(u, v); %修改标号值
                parent(v) = u;
            end
        end
    end
    mypath = [];
    if parent(db) ~= 0 %如果存在路!
        t = db; mypath = [db];
        while t ~= sb
            p = parent(t);
            mypath = [p mypath];
            t = p;
        end
    end
    mydistance = distance(db);
    path_new=zeros(length(mypath),2);
    for i=1:length(mypath)
        path_new(i,1)=path_raw(mypath(i),1);
        path_new(i,2)=path_raw(mypath(i),2);
    end

%distanceCost程序段
function h=distanceCost(a,b)
h = sqrt((a(:,1)-b(:,1)).^2 + (a(:,2)-b(:,2)).^2 );

%feasiblePoint程序段
function feasible=feasiblePoint(point,map)
feasible=true;
% check if collission-free spot and inside maps
if ~(point(1)>=1 && point(1)<=size(map,1) && point(2)>=1 && point(2)<=size(map,2) && map(point(1),point(2))==1)
    feasible=false;
end

%checkPath程序段
function feasible=checkPath(n,newPos,map)
feasible=true;
dir=atan2(newPos(1)-n(1),newPos(2)-n(2));
for r=0:0.5:sqrt(sum((n-newPos).^2))
    posCheck=n+r.*[sin(dir) cos(dir)];
    if ~(feasiblePoint(ceil(posCheck),map) && feasiblePoint(floor(posCheck),map) && ... 
            feasiblePoint([ceil(posCheck(1)) floor(posCheck(2))],map) && feasiblePoint([floor(posCheck(1)) ceil(posCheck(2))],map))
        feasible=false;break;
    end
    if ~feasiblePoint(newPos,map), feasible=false; end
end


function ReceiveCallback(obj,event) %执行回调的函数
global s;
a = fscanf(s);
disp(a);
I = 'I Received';
disp(I);


% --- Executes on button press in Xqian.
function Xqian_Callback(hObject, eventdata, handles)
% hObject    handle to Xqian (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)
global s;
pause(1);
fprintf(s,'r');
pause(0.1);%时间间隔不宜过小
disp('正在往右走...');

% --- Executes on button press in Yqian.
function Yqian_Callback(hObject, eventdata, handles)
% hObject    handle to Yqian (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)
global s;
pause(1);
fprintf(s,'h');
pause(0.1);%时间间隔不宜过小
disp('正在往前走...');

% --- Executes on button press in Yhou.
function Yhou_Callback(hObject, eventdata, handles)
% hObject    handle to Yhou (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)
global s;
pause(1);
fprintf(s,'t');
pause(0.1);%时间间隔不宜过小
disp('正在往后走...');

% --- Executes on button press in Xhou.
function Xhou_Callback(hObject, eventdata, handles)
% hObject    handle to Xhou (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)
global s;
pause(1);
fprintf(s,'l');
pause(0.1);%时间间隔不宜过小
disp('正在往左走...');

% --- Executes on button press in ting.
function ting_Callback(hObject, eventdata, handles)
% hObject    handle to ting (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)
global s;
pause(1);
fprintf(s,'s');
pause(0.1);%时间间隔不宜过小
disp('电机停止运动...');


% --- Executes on button press in close.
function close_Callback(hObject, eventdata, handles)
% hObject    handle to close (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)
set(handles.pushbutton24,'enable','on');%打开串口的按钮重新可用
global s;%全局变量Scom
display(s)
fprintf('关闭串口成功...');
fclose(s);
delete(s);

% --- Executes on button press in pushbutton24.
function pushbutton24_Callback(hObject, eventdata, handles)
% hObject    handle to pushbutton24 (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)
delete(instrfindall);
global port;
display(port)
global s;
s = serial(port,'BaudRate',9600,'DataBits',8);       % 使用默认设置创建串口sr3
       s.InputBufferSize=2000; %设置好buf的空间，足够最多一次指令返回数据的存储
       s.timeout=3;
       s.BaudRate=9600;
       s.DataBits=8;
       s.Parity='none';
       s.StopBits=1;
fopen(s);                 %打开串口
fprintf("打开串口成功...");
set(handles.pushbutton24,'enable','off');%打开串口的按钮变成灰色，不再可用
set(s,'BytesAvailableFcnMode','Terminator');
set(s,'Terminator','H');                    %设立中断，当串口检测到字符H时触发中断
s.BytesAvailableFcn =@ReceiveCallback; %中断触发后处理回调的事


% --- Executes on selection change in popupmenu1.
function popupmenu1_Callback(hObject, eventdata, handles)
% hObject    handle to popupmenu1 (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)

% Hints: contents = cellstr(get(hObject,'String')) returns popupmenu1 contents as cell array
%        contents{get(hObject,'Value')} returns selected item from popupmenu1
global port
scoms = instrfind; %读取所有存在的端口
if ~isempty(scoms)
    stopasync(scoms); fclose(scoms); delete(scoms);%停止并且删除串口对象
end
vall=get(handles.popupmenu1,'Value');
switch vall
    case 1
        port='com1';
    case 2
        port='com2';
    case 3
        port='com3';
    case 4
        port='com4';
    case 5
        port='com5';
    case 6
        port='com6';
    case 7
        port='com7';
    case 8
        port='com8';
    case 9
        port='com9';
    case 10
        port='com10';
    case 11
        port='com11';
end


% --- Executes during object creation, after setting all properties.
function popupmenu1_CreateFcn(hObject, eventdata, handles)
% hObject    handle to popupmenu1 (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    empty - handles not created until after all CreateFcns called

% Hint: popupmenu controls usually have a white background on Windows.
%       See ISPC and COMPUTER.
if ispc && isequal(get(hObject,'BackgroundColor'), get(0,'defaultUicontrolBackgroundColor'))
    set(hObject,'BackgroundColor','white');
end

% --- Executes on button press in pushbutton25.
function pushbutton25_Callback(hObject, eventdata, handles)
% hObject    handle to pushbutton25 (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)
global s;
pause(1);
fprintf(s,'d');
pause(0.1);%时间间隔不宜过小
disp('电机向下运动...');

% --- Executes on button press in pushbutton26.
function pushbutton26_Callback(hObject, eventdata, handles)
% hObject    handle to pushbutton26 (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)
global s;
pause(1);
fprintf(s,'u');
pause(0.1);%时间间隔不宜过小
disp('电机向上运动...');

% --- Executes on button press in pushbutton27.
function pushbutton27_Callback(hObject, eventdata, handles)
% hObject    handle to pushbutton27 (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)
global s;
pause(1);
fprintf(s,'o');
pause(0.1);%时间间隔不宜过小
disp('电机回零运动中...');
set(handles.pushbutton27,'visible','off');
set(handles.pushbutton1,'visible','on');


% tic;                                % tic;与toc;配合使用能够返回程序运行时间
% bar = waitbar(0,'读取数据中...');    % waitbar显示进度条
% A = randn(1000,1);                  % 随机生成1000行1列数据
% len = length(A);                    % 读取A矩阵长度
% for i = 1:len                       % 循环1000次
%     B(i) = i^2;                     % 求平方，无意义，示例函数
%     str=['计算中...',num2str(100*i/len),'%'];    % 百分比形式显示处理进程,不需要删掉这行代码就行
%     waitbar(i/len,bar,str)                       % 更新进度条bar，配合bar使用
% end
% %close(bar)                % 循环结束可以关闭进度条，个人一般留着不关闭
% toc;                       % tic;与toc;配合使用能够返回程序运行时间
