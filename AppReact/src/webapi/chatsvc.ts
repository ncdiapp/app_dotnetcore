import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { getHeaders } from '../helper/apiServiceHelper';
class ChatService {
  private connection: HubConnection;
  private messageCounter = 0;
  private online: string[] = [];

  constructor() {
    this.connection = new HubConnectionBuilder()
      .withUrl('/chatHub')
      .withAutomaticReconnect()
      .build();

    this.setupConnectionHandlers();
    this.startConnection();
  }

  private async startConnection(): Promise<void> {
    try {
      await this.connection.start();
      console.log('SignalR connection started');
    } catch (err) {
      console.error('Error starting SignalR connection:', err);
      setTimeout(() => this.startConnection(), 5000);
    }
  }

  private setupConnectionHandlers(): void {
    this.connection.on('setOnline', (name: string) => {
      const userElement = document.getElementById('user');
      if (userElement) {
        const p = document.createElement('p');
        p.id = name;
        p.className = 'text-center';
        p.style.fontSize = '30px';
        p.textContent = `*${name}`;
        userElement.appendChild(p);
      }
      this.online.push(name);
    });

    this.connection.on('setOffline', (name: string) => {
      const person = document.getElementById(name);
      if (person) {
        person.remove();
      }
      const index = this.online.indexOf(name);
      if (index > -1) {
        this.online.splice(index, 1);
      }
    });

    this.connection.on('setColor', (colorStr: string) => {
      const messageElement = document.getElementById(`message_${this.messageCounter - 1}`);
      if (messageElement) {
        messageElement.style.fontSize = '30px';
        messageElement.style.color = colorStr;
      }
    });

    this.connection.on('enter', (name: string, message: string) => {
      console.log(`${name}: ${message}`);
      const id = `message_${this.messageCounter}`;
      const discussionElement = document.getElementById('discussion');
      if (discussionElement) {
        const p = document.createElement('p');
        p.id = id;
        p.innerHTML = `<strong>${name}: ${message}</strong>`;
        p.style.fontSize = '30px';
        discussionElement.appendChild(p);
        this.messageCounter++;
        
        setTimeout(() => {
          p.scrollIntoView();
        }, 100);
      }
    });
  }

  async broadcastMessage(name: string, message: string): Promise<void> {
    try {
      await this.connection.invoke('send', name, message);
    } catch (err) {
      console.error('Error broadcasting message:', err);
    }
  }

  async goOnline(name: string): Promise<void> {
    try {
      await this.connection.invoke('setOnline', name);
    } catch (err) {
      console.error('Error going online:', err);
    }
  }

  async goOffline(name: string): Promise<void> {
    try {
      await this.connection.invoke('setOffline', name);
    } catch (err) {
      console.error('Error going offline:', err);
    }
  }
}

export const chatService = new ChatService(); 