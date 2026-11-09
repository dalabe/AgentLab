import { Component } from '@angular/core';
import { ChatPage } from './chat/chat-page';

@Component({
  selector: 'app-root',
  imports: [ChatPage],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {}
