import socket
import csv
TCP_IP = '192.168.43.97'
TCP_PORT = 5005

if __name__ == '__main__':
    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    s.bind((TCP_IP, TCP_PORT))
    s.listen(10)
    client_socket, addr = s.accept()
    while True:
        print(client_socket)
        array = []
        f = open("POSEDATA.csv","r")
        data = csv.reader(f,delimiter=' ')
        for e in data:  
            array.extend(e)
        array = " ".join(array)
        
        print(array)
        s.sendall(array,encoding = 'utf-8')
        print("clear")
        s.close()