import React, { useState, useEffect } from 'react'
import ReservationForm from './components/ReservationForm';
import MyReservations from './components/MyReservations';
import RoomSchedule from './components/RoomSchedule';

// --- TYPY DANYCH ---
interface OccupiedDate {
  CheckInDate: string;
  CheckOutDate: string;
}

interface Room {
  Id: number;
  RoomNumber: string;
  Type: string;
  Capacity: number;
  PricePerNight: number;
  OccupiedDates: OccupiedDate[]; // Nowe pole z backendu
}

interface MyReservation {
  Id: string;
  RoomNumber: string;
  RoomType: string;
  CheckInDate: string;
  CheckOutDate: string;
  TotalPrice: number;
}

// Pobieranie URL API
const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:7071/api';

function App() {
  // --- STATE ---
  const [view, setView] = useState<'book' | 'check' | 'schedule'>('book'); // Dodano 'schedule'
  
  const [rooms, setRooms] = useState<Room[]>([]);
  const [formData, setFormData] = useState({
    guestName: '',
    guestEmail: '',
    roomId: '',
    checkInDate: '',
    checkOutDate: ''
  });

  const [checkEmail, setCheckEmail] = useState('');
  const [myReservations, setMyReservations] = useState<MyReservation[]>([]);

  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState<{type: 'success'|'error', text: string} | null>(null);

  // --- EFEKTY ---
  // Funkcja pobierająca pokoje (używana przy starcie i po rezerwacji, żeby odświeżyć grafik)
  const fetchRooms = () => {
    fetch(`${API_URL}/rooms`)
      .then(res => res.json())
      .then(data => setRooms(data))
      .catch(err => console.error("Błąd pobierania pokoi", err));
  };

  useEffect(() => {
    fetchRooms();
  }, []);

  // --- OBSŁUGA ZMIANY FORMULARZA --- 
  const handleFormChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    setFormData(prevData => ({ ...prevData, [name]: value }));
  };

  // --- OBSŁUGA FORMULARZA REZERWACJI ---
  const handleBookSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setMessage(null);

    const payload = {
      GuestName: formData.guestName,
      GuestEmail: formData.guestEmail,
      RoomId: parseInt(formData.roomId),
      CheckInDate: formData.checkInDate,
      CheckOutDate: formData.checkOutDate
    };

    try {
      const res = await fetch(`${API_URL}/reservation`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });

      if (res.status === 409) {
        throw new Error("Ten pokój jest zajęty w wybranym terminie!");
      }
      if (!res.ok) throw new Error("Błąd rezerwacji.");

      const data = await res.json();
      const price = data.Price || data.price;
      setMessage({ type: 'success', text: `Sukces! ID: ${data.ReservationId}. Cena: ${price} PLN` });
      
      // Odśwież grafik po udanej rezerwacji!
      fetchRooms();

    } catch (err: any) {
      setMessage({ type: 'error', text: err.message });
    } finally {
      setLoading(false);
    }
  };

  // --- OBSŁUGA SPRAWDZANIA REZERWACJI ---
  const handleCheckSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      const res = await fetch(`${API_URL}/my-reservations/${checkEmail}`);
      if(res.ok) {
        const data = await res.json();
        setMyReservations(data);
        if(data.length === 0) setMessage({type: 'error', text: "Brak rezerwacji dla tego maila."});
        else setMessage(null);
      }
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  }

  // Helper do formatowania daty
  const formatDate = (dateString: string) => new Date(dateString).toLocaleDateString('pl-PL');

  return (
    <div style={{ display: 'flex', justifyContent: 'center', width: '100%', minHeight: '100vh' }}>
    <div className="container mt-4 mb-5" style={{ maxWidth: '900px' }}>
      
      <div className="text-center mb-4">
        <h2>🏨 Smart Hotel Assistant</h2>
      </div>

      {/* NAVBAR */}
      <ul className="nav nav-pills nav-fill mb-4 shadow-sm p-2 bg-white rounded">
        <li className="nav-item">
          <button className={`nav-link ${view === 'book' ? 'active' : ''}`} onClick={() => setView('book')}>
            ➕ Nowa Rezerwacja
          </button>
        </li>
        <li className="nav-item">
          <button className={`nav-link ${view === 'check' ? 'active' : ''}`} onClick={() => setView('check')}>
            🔍 Moje Rezerwacje
          </button>
        </li>
        <li className="nav-item">
          <button className={`nav-link ${view === 'schedule' ? 'active' : ''}`} onClick={() => setView('schedule')}>
            📅 Grafik / Dostępność
          </button>
        </li>
      </ul>

      <div className="card shadow">
        <div className="card-body">
          
          {/* WIDOK: REZERWACJA */}
          {view === 'book' && (
            <ReservationForm 
              formData={formData}
              rooms={rooms}
              loading={loading}
              onFormChange={handleFormChange}
              onSubmit={handleBookSubmit}
            />
          )}

          {/* WIDOK: MOJE REZERWACJE */}
          {view === 'check' && (
            <MyReservations
              checkEmail={checkEmail}
              myReservations={myReservations}
              loading={loading}
              onEmailChange={e => setCheckEmail(e.target.value)}
              onSubmit={handleCheckSubmit}
              formatDate={formatDate}
            />
          )}

          {/* WIDOK: GRAFIK (NOWY) */}
          {view === 'schedule' && (
            <RoomSchedule
              rooms={rooms}
              formatDate={formatDate}
            />
          )}

          {/* KOMUNIKATY GLOBALNE */}
          {message && (
            <div className={`alert mt-3 alert-${message.type === 'success' ? 'success' : 'danger'}`}>
              {message.text}
            </div>
          )}
        
        </div>
      </div>
    </div>
    </div>
  )
}

export default App