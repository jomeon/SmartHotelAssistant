import React from 'react';

interface MyReservation {
  Id: string;
  RoomNumber: string;
  RoomType: string;
  CheckInDate: string;
  CheckOutDate: string;
  TotalPrice: number;
}

interface MyReservationsProps {
  checkEmail: string;
  myReservations: MyReservation[];
  loading: boolean;
  onEmailChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
  onSubmit: (e: React.FormEvent) => void;
  formatDate: (dateString: string) => string;
}

const MyReservations: React.FC<MyReservationsProps> = ({
  checkEmail,
  myReservations,
  loading,
  onEmailChange,
  onSubmit,
  formatDate
}) => {
  return (
    <div>
      <h4 className="mb-3">Wyszukaj rezerwacje</h4>
      <form onSubmit={onSubmit} className="d-flex gap-2 mb-4">
        <input required type="email" className="form-control" placeholder="Podaj swój e-mail..."
          value={checkEmail} onChange={onEmailChange} />
        <button type="submit" className="btn btn-secondary" disabled={loading}>Szukaj</button>
      </form>

      {myReservations.length > 0 && (
        <table className="table table-striped">
          <thead>
            <tr>
              <th>Pokój</th>
              <th>Termin</th>
              <th>Koszt</th>
            </tr>
          </thead>
          <tbody>
            {myReservations.map(res => (
              <tr key={res.Id}>
                <td>{res.RoomNumber} <small className="text-muted">({res.RoomType})</small></td>
                <td>{formatDate(res.CheckInDate)} ➝ {formatDate(res.CheckOutDate)}</td>
                <td>{res.TotalPrice} PLN</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      {myReservations.length === 0 && !loading && checkEmail && (
        <div className="alert alert-info">Brak rezerwacji dla podanego adresu e-mail.</div>
      )}
    </div>
  );
};

export default MyReservations;
